// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;
using ZeroInstall.Store.Deployment;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Copies all implementations in a <see cref="Selections"/> document into a directory and generates a launcher script that sets up the environment the same way <see cref="Executor"/> would.
/// The resulting directory can be run without Zero Install and can be moved to a different location.
/// </summary>
/// <param name="implementationStore">Used to locate the implementations to copy.</param>
/// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
public class Materializer(IImplementationStore implementationStore, ITaskHandler handler)
{
    /// <summary>
    /// The operating system the launcher scripts will run on. Defaults to the current system.
    /// </summary>
    public OS TargetOS { get; init; } = Architecture.CurrentSystem.OS;

    /// <summary>
    /// Materializes a set of selections into a directory.
    /// </summary>
    /// <param name="selections">The selections to materialize. All implementations must already be cached.</param>
    /// <param name="targetDir">The directory to place the implementations and launcher scripts in. Created if it does not exist yet.</param>
    /// <param name="launcherName">The file name for the main launcher script, without a file extension.</param>
    /// <returns>The full path of the file to run to start the program, including a file extension if <see cref="TargetOS"/> requires one.</returns>
    /// <exception cref="ImplementationNotFoundException">One of the <see cref="Implementation"/>s is not cached yet.</exception>
    /// <exception cref="ExecutorException">The <see cref="Selections"/> cannot be expressed as a launcher script.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while reading or writing files.</exception>
    /// <exception cref="UnauthorizedAccessException">Read or write access to a file is not permitted.</exception>
    public string Materialize(Selections selections, string targetDir, string launcherName)
    {
        #region Sanity checks
        if (selections == null) throw new ArgumentNullException(nameof(selections));
        if (string.IsNullOrEmpty(targetDir)) throw new ArgumentNullException(nameof(targetDir));
        if (string.IsNullOrEmpty(launcherName)) throw new ArgumentNullException(nameof(launcherName));
        if (launcherName.IndexOfAny(['/', '\\']) >= 0 || launcherName is "." or ".." or LauncherScriptBuilder.BinDir) throw new ArgumentException($"Invalid launcher name: {launcherName}", nameof(launcherName));
        #endregion

        var implementations = LocateImplementations(selections);

        var scriptBuilder = LauncherScriptBuilder.For(TargetOS, implementationStore, x => implementations[x.InterfaceUri].DirName);
        scriptBuilder.Inject(selections);
        string launcher = scriptBuilder.GetLauncher();
        var executables = scriptBuilder.GetExecutables().ToList();

        Directory.CreateDirectory(targetDir);
        DeployImplementations(implementations.Values, targetDir);
        string launcherPath = Paths.Combine(targetDir, launcherName + scriptBuilder.LauncherExtension);
        WriteExecutable(launcherPath, [..scriptBuilder.Encoding.GetPreamble(), ..scriptBuilder.Encoding.GetBytes(launcher)]);
        if (scriptBuilder.GetWrapper() is {} wrapper)
        {
            launcherPath = Paths.Combine(targetDir, launcherName + wrapper.extension);
            WriteExecutable(launcherPath, wrapper.content);
        }
        WriteExecutables(executables, Paths.Combine(targetDir, LauncherScriptBuilder.BinDir));
        RemoveStaleImplementations(targetDir, keep: implementations.Values.Select(x => x.DirName));
        return launcherPath;
    }

    /// <summary>
    /// An implementation to be copied into the target directory.
    /// </summary>
    /// <param name="SourcePath">The directory containing the implementation.</param>
    /// <param name="Manifest">The manifest describing the contents of <paramref name="SourcePath"/>.</param>
    /// <param name="DirName">The name of the subdirectory to place the implementation in. Named after the manifest digest.</param>
    private sealed record ImplementationCopy(string SourcePath, Manifest Manifest, string DirName);

    private Dictionary<FeedUri, ImplementationCopy> LocateImplementations(Selections selections)
    {
        var result = new Dictionary<FeedUri, ImplementationCopy>();
        foreach (var implementation in selections.Implementations)
        {
            if (implementation.ID.StartsWith(ExternalImplementation.PackagePrefix))
            {
                Log.Warn($"Not copying {implementation.ID} because it is provided by a native package manager");
                continue;
            }

            string sourcePath = implementationStore.GetPath(implementation);
            result[implementation.InterfaceUri] = string.IsNullOrEmpty(implementation.LocalPath)
                ? FromStore(sourcePath)
                : FromLocalPath(sourcePath);
        }
        return result;
    }

    /// <summary>
    /// Uses the manifest the implementation store keeps next to each implementation. Store directories are named after their manifest digest.
    /// </summary>
    private static ImplementationCopy FromStore(string sourcePath)
    {
        string dirName = Path.GetFileName(sourcePath);
        var manifest = Manifest.Load(Paths.Combine(sourcePath, Manifest.ManifestFile), ManifestFormat.FromPrefix(dirName));
        return new(sourcePath, manifest, dirName);
    }

    /// <summary>
    /// Generates a manifest for an implementation in a local directory, which is not covered by a stored manifest.
    /// </summary>
    private ImplementationCopy FromLocalPath(string sourcePath)
    {
        var builder = new ManifestBuilder(ManifestFormat.Sha256New);
        handler.RunTask(new ReadDirectory(sourcePath, builder));
        return new(sourcePath, builder.Manifest, builder.Manifest.CalculateDigest());
    }

    private void DeployImplementations(IEnumerable<ImplementationCopy> implementations, string targetDir)
    {
        var operations = new List<DeployDirectory>();
        var seen = new HashSet<string>();
        try
        {
            foreach (var implementation in implementations)
            {
                if (!seen.Add(implementation.DirName)) continue;

                string destinationPath = Paths.Combine(targetDir, implementation.DirName);

                // The manifest file is the last file moved into place, so its presence indicates a complete copy
                if (File.Exists(Paths.Combine(destinationPath, Manifest.ManifestFile)))
                {
                    Log.Debug($"Implementation already present in {destinationPath}");
                    continue;
                }

                var operation = new DeployDirectory(implementation.SourcePath, implementation.Manifest, destinationPath, handler);
                operations.Add(operation);
                operation.Stage();
            }

            foreach (var operation in operations)
                operation.Commit();
        }
        finally
        {
            foreach (var operation in operations)
                operation.Dispose();
        }
    }

    private static void WriteExecutables(IReadOnlyCollection<(string name, byte[] content)> executables, string binDir)
    {
        if (executables.Count != 0) Directory.CreateDirectory(binDir);

        foreach ((string name, byte[] content) in executables)
            WriteExecutable(Paths.Combine(binDir, name), content);

        // Remove executables from earlier runs that are no longer needed
        if (Directory.Exists(binDir))
        {
            var names = executables.Select(x => x.name).ToHashSet();
            foreach (string path in Directory.GetFiles(binDir).Where(x => !names.Contains(Path.GetFileName(x))))
                File.Delete(path);
            if (Directory.GetFileSystemEntries(binDir).Length == 0)
                Directory.Delete(binDir);
        }
    }

    private static void WriteExecutable(string path, byte[] content)
    {
        using var atomic = new AtomicWrite(path);
        File.WriteAllBytes(atomic.WritePath, content);
        ImplFileUtils.SetExecutable(atomic.WritePath);
        atomic.Commit();
    }

    /// <summary>
    /// Removes implementations that were materialized earlier but are no longer selected.
    /// Only touches directories that are named after a manifest digest and contain a manifest file.
    /// </summary>
    private void RemoveStaleImplementations(string targetDir, IEnumerable<string> keep)
    {
        var keepSet = keep.ToHashSet();
        foreach (string path in Directory.GetDirectories(targetDir))
        {
            string dirName = Path.GetFileName(path);
            if (keepSet.Contains(dirName)) continue;

            ManifestFormat format;
            try
            {
                format = ManifestFormat.FromPrefix(dirName);
            }
            catch (NotSupportedException)
            {
                continue;
            }

            if (Manifest.TryLoad(Paths.Combine(path, Manifest.ManifestFile), format) is not {} manifest) continue;

            Log.Info($"Removing implementation that is no longer selected: {path}");
            using var operation = new ClearDirectory(path, manifest, handler);
            operation.Stage();
            operation.Commit();
        }
    }
}
