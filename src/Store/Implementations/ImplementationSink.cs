// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using NanoByte.Common.Native;
using NanoByte.Common.Threading;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Accepts implementations and stores them.
/// </summary>
public class ImplementationSink : MarshalNoTimeout, IImplementationSink
{
    /// <summary>Controls whether implementation directories are made write-protected once added to prevent unintentional modification (which would invalidate the manifest digests).</summary>
    protected readonly bool UseWriteProtection;

    /// <summary>Indicates whether this implementation sink does not support write access.</summary>
    protected readonly bool ReadOnly;

    /// <summary>
    /// The path to the underlying directory in the file system.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Creates a new implementation sink using a specific path to a directory.
    /// </summary>
    /// <param name="path">A fully qualified directory path. The directory will be created if it doesn't exist yet.</param>
    /// <param name="useWriteProtection">Controls whether implementation directories are made write-protected once added to prevent unintentional modification (which would invalidate the manifest digests).</param>
    /// <exception cref="NotSupportedException">The underlying filesystem can not store file-changed times accurate to the second. Probably using FAT32 instead of NTFS.</exception>
    /// <exception cref="IOException">The <paramref name="path"/> could not be created or the underlying filesystem can not store file-changed times accurate to the second.</exception>
    /// <exception cref="UnauthorizedAccessException">Creating the <paramref name="path"/> is not permitted.</exception>
    public ImplementationSink(string path, bool useWriteProtection = true)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        #endregion

        try
        {
            Path = System.IO.Path.GetFullPath(path);
            if (!Directory.Exists(Path)) Directory.CreateDirectory(Path);
        }
        #region Error handling
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            // Wrap exception since only certain exception types are allowed
            throw new IOException(ex.Message, ex);
        }
        #endregion

        UseWriteProtection = useWriteProtection;

        try
        {
            if (FileUtils.DetermineTimeAccuracy(Path) > 0)
                throw new NotSupportedException(Resources.InsufficientFSTimeAccuracy);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ReadOnly = true;
        }
    }

    /// <inheritdoc/>
    public void Add(ManifestDigest manifestDigest, Action<IBuilder> build)
    {
        #region Sanity checks
        if (build == null) throw new ArgumentNullException(nameof(build));
        #endregion

        if (manifestDigest.AvailableDigests.Any(digest => Directory.Exists(System.IO.Path.Combine(Path, digest)))) throw new ImplementationAlreadyInStoreException(manifestDigest);
        string expectedDigest = manifestDigest.Best ?? throw new NotSupportedException(Resources.NoKnownDigestMethod);
        Log.Debug($"Storing implementation {expectedDigest} in {this}");
        var format = ManifestFormat.FromPrefix(manifestDigest.Best);

        // Place files in temp directory until digest is verified
        using var tempDir = new TemporaryDirectory("0install-extract", Path);
        Log.Debug("Temp directory for extracting: " + tempDir);

        var builder = new ManifestBuilder(format);
        build(new DirectoryBuilder(tempDir, builder));
        var manifest = ImplementationStoreUtils.Verify(builder.Manifest, expectedDigest);

        if (manifest == null)
        {
            throw new DigestMismatchException(
                expectedDigest, actualDigest: builder.Manifest.CalculateDigest(),
                actualManifest: builder.Manifest);
        }
        manifest.Save(System.IO.Path.Combine(tempDir, Manifest.ManifestFile));

        string target = System.IO.Path.Combine(Path, expectedDigest);
        lock (_renameLock) // Prevent race-conditions when adding the same digest twice
        {
            if (Directory.Exists(target)) throw new ImplementationAlreadyInStoreException(manifestDigest);

            // Move directory to final destination
            try
            {
                Directory.Move(tempDir, target);
            }
            catch (IOException ex) when (ex.Message.Contains("already exists") || Directory.Exists(target))
            {
                throw new ImplementationAlreadyInStoreException(manifestDigest);
            }
        }

        if (UseWriteProtection)
        {
            EnableWriteProtection(target);
            DeployDeleteInfoFile();
        }
    }

    private readonly object _renameLock = new();

    /// <summary>
    /// Makes a directory read-only using platform-specific mechanisms. Logs any errors and continues.
    /// </summary>
    /// <param name="path">The directory to protect.</param>
    private static void EnableWriteProtection(string path)
    {
        try
        {
            Log.Debug("Enabling write protection for: " + path);
            FileUtils.EnableWriteProtection(path);
        }
        #region Error handling
        catch (IOException)
        {
            Log.Warn(string.Format(Resources.UnableToWriteProtect, path));
        }
        catch (UnauthorizedAccessException)
        {
            // Only warn if even an Admin is unable to set ACLs
            if (WindowsUtils.IsWindowsNT && WindowsUtils.IsAdministrator) Log.Warn(string.Format(Resources.UnableToWriteProtect, path));
            else Log.Info(string.Format(Resources.UnableToWriteProtect, path));
        }
        catch (InvalidOperationException)
        {
            Log.Warn(string.Format(Resources.UnableToWriteProtect, path));
        }
        #endregion
    }

    /// <summary>
    /// Deploys a file explaining to users how to delete files with write protection.
    /// </summary>
    private void DeployDeleteInfoFile()
    {
        string filePath = System.IO.Path.Combine(Path, Resources.DeleteInfoFileName + ".txt");
        string escapedDirPath = Path.EscapeArgument();

        try
        {
            File.WriteAllText(filePath,
                string.Format(Resources.DeleteInfoFileContent,
                    "0install store remove IMPLEMENTATION-ID",
                    $"0install store purge {escapedDirPath}",
                    WindowsUtils.IsWindowsNT
                        ? $"icacls {escapedDirPath} /t /q /c /reset; rm -Recurse {escapedDirPath}"
                        : $"chmod -R u+w {escapedDirPath} && rm -rf {escapedDirPath}"),
                Encoding.UTF8);
            EnableWriteProtection(filePath);
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {}
    }
}
