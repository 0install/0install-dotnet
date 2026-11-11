// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
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
    /// Create links to the implementation directories instead of copying them.
    /// </summary>
    /// <remarks>This is faster and saves disk space, but the resulting directory stops working when the implementations are removed.</remarks>
    public bool UseLinks { get; init; }

    /// <summary>
    /// Remove content from earlier runs that is no longer needed: implementations that are no longer selected and unneeded files in <see cref="LauncherScriptBuilder.BinDir"/>.
    /// Also replace existing copies with links or vice versa to match <see cref="UseLinks"/>.
    /// </summary>
    /// <remarks>Without this, existing files are left as-is, apart from the ones being regenerated.</remarks>
    public bool Clean { get; init; }

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
        if (launcherName.IndexOfAny(['/', '\\']) >= 0
         || launcherName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
         || launcherName is "." or ".."
         || launcherName.Equals(LauncherScriptBuilder.BinDir, StringComparison.OrdinalIgnoreCase)) // May collide with the directory on case-insensitive file systems
            throw new ArgumentException($"Invalid launcher name: {launcherName}", nameof(launcherName));
        #endregion

        targetDir = Path.GetFullPath(targetDir);
        var implementations = LocateImplementations(selections, targetDir);

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
        string binDir = Paths.Combine(targetDir, LauncherScriptBuilder.BinDir);
        WriteExecutables(executables, binDir);
        if (Clean)
        {
            RemoveStaleExecutables(binDir, keep: executables.Select(x => x.name));
            RemoveStaleImplementations(targetDir, keep: implementations.Values.Select(x => x.DirName));
        }
        return launcherPath;
    }

    /// <summary>
    /// An implementation to be copied or linked into the target directory.
    /// </summary>
    /// <param name="SourcePath">The directory containing the implementation.</param>
    /// <param name="Manifest">The manifest describing the contents of <paramref name="SourcePath"/>.</param>
    /// <param name="DirName">The name of the subdirectory or link to place the implementation in. Named after the manifest digest.</param>
    private sealed record ImplementationCopy(string SourcePath, Manifest Manifest, string DirName);

    private Dictionary<FeedUri, ImplementationCopy> LocateImplementations(Selections selections, string targetDir)
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
            if (string.IsNullOrEmpty(implementation.LocalPath))
                result[implementation.InterfaceUri] = FromStore(sourcePath);
            else
            {
                // Otherwise each run would copy the output of the previous one, nested ever deeper
                if (IsSameOrInside(targetDir, sourcePath))
                    throw new IOException($"Cannot materialize into {targetDir} because it is inside the local implementation directory {sourcePath}.");
                result[implementation.InterfaceUri] = FromLocalPath(sourcePath);
            }
        }
        return result;
    }

    /// <summary>
    /// Determines whether <paramref name="path"/> is the same as or a subdirectory of <paramref name="dir"/>.
    /// </summary>
    private static bool IsSameOrInside(string path, string dir)
    {
        string Normalize(string x) => Path.GetFullPath(x).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Normalize(path).StartsWith(Normalize(dir), WindowsUtils.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
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

                if (UseLinks)
                {
                    LinkImplementation(implementation, destinationPath);
                    continue;
                }

                if (IsLink(destinationPath))
                {
                    if (!Clean)
                    {
                        Log.Info($"Keeping existing link instead of replacing it with a copy: {destinationPath}");
                        continue;
                    }

                    Log.Debug($"Replacing link with copy: {destinationPath}");
                    DeleteLink(destinationPath);
                }

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

    private void LinkImplementation(ImplementationCopy implementation, string destinationPath)
    {
        if (IsLink(destinationPath))
        {
            // Recreated rather than kept, since the implementation may have moved
            DeleteLink(destinationPath);
        }
        else if (Directory.Exists(destinationPath))
        {
            if (!Clean)
            {
                Log.Info($"Keeping existing copy instead of replacing it with a link: {destinationPath}");
                return;
            }

            Log.Debug($"Replacing copy with link: {destinationPath}");
            using (var operation = new ClearDirectory(destinationPath, implementation.Manifest, handler))
            {
                operation.Stage();
                operation.Commit();
            }
            if (Directory.Exists(destinationPath))
                throw new IOException($"Cannot replace {destinationPath} with a link because it contains files that are not part of the implementation.");
        }

        FileUtils.CreateDirectoryLink(destinationPath, Path.GetFullPath(implementation.SourcePath));
    }

    /// <summary>
    /// Determines whether a link exists at a path.
    /// </summary>
    private static bool IsLink(string path)
    {
        try
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes a link without following it.
    /// </summary>
    private static void DeleteLink(string path)
    {
        if (File.GetAttributes(path).HasFlag(FileAttributes.Directory)) Directory.Delete(path, recursive: false);
        else File.Delete(path);
    }

    private static void WriteExecutables(IReadOnlyCollection<(string name, byte[] content)> executables, string binDir)
    {
        if (executables.Count != 0) Directory.CreateDirectory(binDir);

        foreach ((string name, byte[] content) in executables)
            WriteExecutable(Paths.Combine(binDir, name), content);
    }

    /// <summary>
    /// Removes executables from earlier runs that are no longer needed. The directory is reserved for generated executables, so any other files in it are removed too.
    /// </summary>
    private static void RemoveStaleExecutables(string binDir, IEnumerable<string> keep)
    {
        if (!Directory.Exists(binDir)) return;

        var keepSet = keep.ToHashSet();
        foreach (string path in Directory.GetFiles(binDir).Where(x => !keepSet.Contains(Path.GetFileName(x))))
            File.Delete(path);
        if (Directory.GetFileSystemEntries(binDir).Length == 0)
            Directory.Delete(binDir);
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
    /// </summary>
    private void RemoveStaleImplementations(string targetDir, IEnumerable<string> keep)
    {
        var keepSet = keep.ToHashSet();
        foreach (string path in Directory.GetFileSystemEntries(targetDir)) // Note: Dangling links are not listed as directories on Unix
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

            if (IsLink(path))
            {
                Log.Info($"Removing link to implementation that is no longer selected: {path}");
                DeleteLink(path);
                continue;
            }
            if (!Directory.Exists(path)) continue;

            if (Manifest.TryLoad(Paths.Combine(path, Manifest.ManifestFile), format) is not {} manifest) continue;

            // Only the prefix was checked so far, e.g. a renamed copy kept as a backup should not be touched
            if (manifest.CalculateDigest() != dirName)
            {
                Log.Debug($"Not removing {path} because its name does not match its manifest digest");
                continue;
            }

            Log.Info($"Removing implementation that is no longer selected: {path}");
            using var operation = new ClearDirectory(path, manifest, handler);
            operation.Stage();
            operation.Commit();
        }
    }
}
