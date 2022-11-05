// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Dispatch;
using NanoByte.Common.Native;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Manages a directory that stores implementations. Also known as an implementation cache.
/// </summary>
public partial class ImplementationStore : ImplementationSink, IImplementationStore, IEquatable<ImplementationStore>
{
    private readonly ITaskHandler _handler;

    /// <inheritdoc/>
    public ImplementationStoreKind Kind => ReadOnly ? ImplementationStoreKind.ReadOnly : ImplementationStoreKind.ReadWrite;

    /// <summary>
    /// Creates a new implementation store using a specific path to a directory.
    /// </summary>
    /// <param name="path">A fully qualified directory path. The directory will be created if it doesn't exist yet.</param>
    /// <param name="handler">A callback object used when the the user is to be informed about progress or asked questions.</param>
    /// <param name="useWriteProtection">Controls whether implementation directories are made write-protected once added to the store to prevent unintentional modification (which would invalidate the manifest digests).</param>
    /// <exception cref="IOException">The <paramref name="path"/> could not be created or the underlying filesystem can not store file-changed times accurate to the second.</exception>
    /// <exception cref="UnauthorizedAccessException">Creating the <paramref name="path"/> is not permitted.</exception>
    public ImplementationStore(string path, ITaskHandler handler, bool useWriteProtection = true)
        : base(path, useWriteProtection)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Determines whether the store contains a local copy of an implementation identified by a specific <see cref="ManifestDigest"/>.
    /// </summary>
    /// <param name="manifestDigest">The digest of the implementation to check for.</param>
    /// <returns>
    ///   <c>true</c> if the specified implementation is available in the store;
    ///   <c>false</c> if the specified implementation is not available in the store or if read access to the store is not permitted.
    /// </returns>
    /// <remarks>If read access to the store is not permitted, no exception is thrown.</remarks>
    public bool Contains(ManifestDigest manifestDigest)
        => manifestDigest.AvailableDigests.Any(digest => Directory.Exists(System.IO.Path.Combine(Path, digest)));

    /// <inheritdoc/>
    public string? GetPath(ManifestDigest manifestDigest)
        => manifestDigest.AvailableDigests.Select(digest => System.IO.Path.Combine(Path, digest)).FirstOrDefault(Directory.Exists);

    /// <inheritdoc/>
    public IEnumerable<ManifestDigest> ListAll()
    {
        if (!Directory.Exists(Path)) return Enumerable.Empty<ManifestDigest>();

        var result = new List<ManifestDigest>();
        foreach (string path in Directory.GetDirectories(Path))
        {
            var digest = new ManifestDigest();
            digest.TryParse(System.IO.Path.GetFileName(path));
            if (digest.AvailableDigests.Any()) result.Add(digest);
        }
        return result;
    }

    /// <inheritdoc/>
    public IEnumerable<string> ListAllTemp()
    {
        if (!Directory.Exists(Path)) return Enumerable.Empty<string>();

        var result = new List<string>();
        foreach (string path in Directory.GetDirectories(Path))
        {
            try
            {
                // ReSharper disable once ObjectCreationAsStatement
                new ManifestDigest(System.IO.Path.GetFileName(path));
            }
            catch (NotSupportedException)
            {
                // Anything that is not a valid digest is considered a temp directory
                result.Add(path);
            }
        }
        return result;
    }

    /// <inheritdoc/>
    public void Verify(ManifestDigest manifestDigest)
    {
        try
        {
            ImplementationStoreUtils.Verify(GetPath(manifestDigest) ?? throw new ImplementationNotFoundException(manifestDigest), manifestDigest, _handler);
        }
        catch (DigestMismatchException ex) when (ex.ExpectedDigest != null)
        {
            Log.Info(ex.LongMessage);
            if (_handler.Ask(
                    question: string.Format(Resources.ImplementationDamaged + Environment.NewLine + Resources.ImplementationDamagedAskRemove, ex.ExpectedDigest),
                    defaultAnswer: false, alternateMessage: string.Format(Resources.ImplementationDamaged + Environment.NewLine + Resources.ImplementationDamagedBatchInformation, ex.ExpectedDigest)))
                Remove(new ManifestDigest(ex.ExpectedDigest));
        }
    }

    /// <inheritdoc/>
    public bool Remove(ManifestDigest manifestDigest)
    {
        if (GetPath(manifestDigest) is {} path)
        {
            if (MissingAdminRights) throw new NotAdminException(Resources.MustBeAdminToRemove);
            Log.Info(string.Format(Resources.DeletingImplementation, manifestDigest));
            return RemoveInner(path);
        }
        else return false;
    }

    /// <inheritdoc />
    public void Purge()
    {
        var paths = Directory.GetDirectories(Path).Where(path =>
        {
            var digest = new ManifestDigest();
            digest.TryParse(System.IO.Path.GetFileName(path));
            return digest.AvailableDigests.Any();
        }).ToList();

        if (paths.Count != 0)
        {
            if (MissingAdminRights) throw new NotAdminException(Resources.MustBeAdminToRemove);

            _handler.RunTask(ForEachTask.Create(
                name: string.Format(Resources.DeletingDirectory, Path),
                target: paths,
                work: path => RemoveInner(path, allowAutoShutdown: true)));
        }

        RemoveDeleteInfoFile();
    }

    private bool RemoveInner(string path, bool allowAutoShutdown = false)
    {
        if (FileUtils.PathEquals(path, Locations.InstallBase))
        {
            Log.Warn(Resources.NoStoreSelfRemove);
            return false;
        }

        if (BlockedByOpenFileHandles(path, allowAutoShutdown))
            return false;

        DisableWriteProtection(path);
        string tempDir = System.IO.Path.Combine(Path, System.IO.Path.GetRandomFileName());
        Directory.Move(path, tempDir);
        Directory.Delete(tempDir, recursive: true);

        return true;
    }

    private bool BlockedByOpenFileHandles(string path, bool allowAutoShutdown)
    {
        if (!WindowsUtils.IsWindowsVista) return false;

        // Prioritize EXEs over DLLs, limit total number of files to avoid slow scan
        List<string> exe = new(), dll = new();
        FileUtils.GetFilesRecursive(path).Bucketize(System.IO.Path.GetExtension).Add("exe", exe).Add("dll", dll);
        var filesToScanFor = exe.Concat(dll).Take(16).ToArray();

        try
        {
            using var restartManager = new WindowsRestartManager();
            restartManager.RegisterResources(filesToScanFor);
            if (restartManager.ListApps(_handler.CancellationToken) is {Length: > 0} apps)
            {
                string appsList = string.Join(Environment.NewLine, apps);
                if (_handler.Ask(Resources.FilesInUse + " " + Resources.FilesInUseAskClose + Environment.NewLine + appsList, defaultAnswer: allowAutoShutdown))
                    restartManager.ShutdownApps(_handler);
                else return true;
            }
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or TimeoutException)
        {
            Log.Warn(string.Format(Resources.FailedToUnlockFiles, path), ex);
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or DllNotFoundException)
        {
            Log.Error("Problem using Windows Restart Manager", ex);
            return true;
        }
        #endregion

        return false;
    }

    /// <summary>
    /// Removes write-protection from a directory read-only using platform-specific mechanisms. Logs any errors and continues.
    /// </summary>
    /// <param name="path">The directory to unprotect.</param>
    internal static void DisableWriteProtection(string path)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        #endregion

        try
        {
            Log.Debug("Disabling write protection for: " + path);
            FileUtils.DisableWriteProtection(path);
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("Failed to disable write protection for: " + path, ex);
        }
        #endregion
    }

    /// <inheritdoc/>
    public long Optimise()
    {
        if (!Directory.Exists(Path)) return 0;
        if (MissingAdminRights) throw new NotAdminException();

        using var run = new OptimiseRun(Path);
        _handler.RunTask(ForEachTask.Create(
            name: string.Format(Resources.FindingDuplicateFiles, Path),
            target: ListAll(),
            work: run.Work));
        return run.SavedBytes;
    }

    private bool MissingAdminRights => ReadOnly && WindowsUtils.IsWindowsNT && !WindowsUtils.IsAdministrator;

    /// <summary>
    /// Creates string representation suitable for console output.
    /// </summary>
    public override string ToString()
        => $"{Kind}: {Path}";

    /// <inheritdoc/>
    public bool Equals(ImplementationStore? other)
        => other != null
        && Path == other.Path;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj == this) return true;
        return obj.GetType() == typeof(ImplementationStore) && Equals((ImplementationStore)obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
        => Path.GetHashCode();
}
