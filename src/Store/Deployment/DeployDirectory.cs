// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

#if NET
using NanoByte.Common.Native;
#endif

namespace ZeroInstall.Store.Deployment;

/// <summary>
/// Deploys/copies files listed in a <see cref="Manifest"/> file to another directory.
/// </summary>
/// <param name="sourcePath">The path of the source directory to copy from.</param>
/// <param name="sourceManifest">The contents of a <see cref="Manifest"/> file describing the source directory.</param>
/// <param name="destinationPath">The path of the destination directory to copy to.</param>
/// <param name="handler">A callback object used when the user needs to be asked questions or informed about IO tasks.</param>
[MustDisposeResource]
public class DeployDirectory(string sourcePath, Manifest sourceManifest, string destinationPath, ITaskHandler handler)
    : DirectoryOperation(sourcePath, sourceManifest, handler)
{
    /// <summary>
    /// The path of the destination directory. May already exist.
    /// </summary>
    public string DestinationPath { get; } = destinationPath;

    /// <summary>
    /// Makes the deployed files and directories accessible to all users on Unixoid systems, regardless of the permissions of the source files and the current umask.
    /// </summary>
    public bool SharedAccess { get; init; }

    private readonly Stack<string> _createdDirectories = [];
    private readonly Stack<(string source, string destination)> _pendingFileRenames = [];

    /// <inheritdoc/>
    protected override void OnStage()
    {
        Log.Debug($"Preparing atomic deployment from {Path} to {DestinationPath}");

        if (!Directory.Exists(DestinationPath))
        {
            Directory.CreateDirectory(DestinationPath);
            _createdDirectories.Push(DestinationPath);
        }

        if (FileUtils.DetermineTimeAccuracy(DestinationPath) > 0)
            throw new IOException(Resources.InsufficientFSTimeAccuracy);

        string manifestPath = Paths.Combine(DestinationPath, Manifest.ManifestFile);
        string tempManifestPath = Randomize(manifestPath);
        _pendingFileRenames.Push((tempManifestPath, manifestPath));
        Manifest.Save(tempManifestPath);
        ApplySharedMode(tempManifestPath, executable: false);

        try
        {
            Handler.RunTask(new ActionTask(Resources.CopyFiles, CopyFromSourceToTemp));
        }
        #region Error handling
        catch (ArgumentException ex)
        {
            // Wrap exception to add context and since only certain exception types are allowed
            throw new IOException("Unable to deploy from directory '{Path}' due to invalid manifest.", ex);
        }
        #endregion
    }

    private void CopyFromSourceToTemp()
    {
        // Symlinks are created last, so their targets already exist (on Windows file and directory links are distinguished at creation time)
        var pendingSymlinks = new List<(string sourcePath, string tempPath)>();

        foreach ((string directoryPath, var directory) in Manifest)
        {
            string dirPath = directoryPath.ToNativePath();
            string sourceDir = Paths.Combine(Path, dirPath);
            string destinationDir = Paths.Combine(DestinationPath, dirPath);
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
                _createdDirectories.Push(destinationDir);
            }
            ApplySharedMode(destinationDir, executable: true);

            foreach ((string path, var element) in directory)
            {
                string sourcePath = Paths.Combine(sourceDir, path);
                string destinationPath = Paths.Combine(destinationDir, path);

                string tempPath = Randomize(destinationPath);
                _pendingFileRenames.Push((tempPath, destinationPath));

                switch (element)
                {
                    case ManifestFile file:
                        File.Copy(sourcePath, tempPath);
                        File.SetLastWriteTimeUtc(tempPath, file.ModifiedTime);

                        if (file is ManifestExecutableFile) ImplFileUtils.SetExecutable(tempPath);
                        ApplySharedMode(tempPath, file is ManifestExecutableFile);
                        break;

                    case ManifestSymlink:
                        pendingSymlinks.Add((sourcePath, tempPath));
                        break;
                }
            }
        }

        foreach ((string sourcePath, string tempPath) in pendingSymlinks)
        {
            if (ImplFileUtils.IsSymlink(sourcePath, out string? symlinkTarget))
                ImplFileUtils.CreateSymlink(tempPath, symlinkTarget, DestinationPath);
        }
    }

    /// <summary>
    /// Applies a fixed Unix mode to a file or directory if <see cref="SharedAccess"/> is enabled.
    /// </summary>
    /// <param name="path">The path of the file or directory. Must not be a symlink.</param>
    /// <param name="executable"><c>true</c> for directories and executable files; <c>false</c> for other files.</param>
    private void ApplySharedMode(string path, bool executable)
    {
#if NET
        if (SharedAccess && UnixUtils.IsUnix)
        {
            const UnixFileMode readWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
            const UnixFileMode execute = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(path, executable ? readWrite | execute : readWrite);
        }
#endif
    }

    /// <inheritdoc/>
    protected override void OnCommit()
    {
        Log.Debug($"Committing atomic deployment to {DestinationPath}");

        UnlockFiles(_pendingFileRenames.Select(x => x.destination).Where(File.Exists));
        Handler.RunTask(new ActionTask(Resources.CopyFiles, MoveFromTempToDestination));
    }

    private void MoveFromTempToDestination()
    {
        // Entries may be directory links on Windows, which File.Exists(), File.Delete() and File.Move() do not handle
        _pendingFileRenames.PopEach(x =>
        {
            ImplFileUtils.DeleteFileOrLink(x.destination);
            ImplFileUtils.MoveFileOrLink(x.source, x.destination);
        });
    }

    /// <inheritdoc/>
    protected override void OnRollback()
    {
        Log.Debug($"Rolling back atomic deployment to {DestinationPath}");

        _pendingFileRenames.PopEach(x => ImplFileUtils.DeleteFileOrLink(x.source));

        _createdDirectories.PopEach(path =>
        {
            if (Directory.Exists(path) && Directory.GetFileSystemEntries(path).Length == 0)
                Directory.Delete(path);
        });
    }
}
