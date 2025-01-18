// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.Deployment;

/// <summary>
/// Deletes files listed in a <see cref="Manifest"/> file from a directory.
/// </summary>
/// <param name="path">The path of the directory to clear.</param>
/// <param name="manifest">The contents of a <see cref="Manifest"/> file describing the directory.</param>
/// <param name="handler">A callback object used when the user needs to be asked questions or informed about IO tasks.</param>
[MustDisposeResource]
public class ClearDirectory(string path, Manifest manifest, ITaskHandler handler)
    : DirectoryOperation(path, manifest, handler)
{
    private readonly Stack<string> _pendingDirectoryDeletes = [];

    private readonly Stack<(string path, string backupPath)> _pendingFilesDeletes = [];

    /// <inheritdoc/>
    protected override void OnStage()
    {
        Log.Debug($"Preparing atomic clearing of directory {Path}");

        _pendingDirectoryDeletes.Push(Path);

        try
        {
            var filesToDelete = GetFilesToDelete().ToList();
            if (filesToDelete.Count == 0) return;

            UnlockFiles(filesToDelete);

            Handler.RunTask(ForEachTask.Create(Resources.DeletingObsoleteFiles, filesToDelete, path =>
            {
                string tempPath = Randomize(path);
                File.Move(path, tempPath);
                _pendingFilesDeletes.Push((path, tempPath));
            }));
        }
        #region Error handling
        catch (ArgumentException ex)
        {
            Log.Warn($"Unable to determine old files to delete in '{Path}' due to invalid manifest", ex);
        }
        #endregion
    }

    private IEnumerable<string> GetFilesToDelete()
    {
        string manifestPath = System.IO.Path.Combine(Path, Manifest.ManifestFile);
        if (File.Exists(manifestPath))
            yield return manifestPath;

        foreach ((string directoryPath, var directory) in Manifest)
        {
            string fullDirectoryPath = System.IO.Path.Combine(Path, directoryPath.ToNativePath());

            if (Directory.Exists(fullDirectoryPath))
                _pendingDirectoryDeletes.Push(fullDirectoryPath);

            foreach (string elementName in directory.Keys)
            {
                string path = System.IO.Path.Combine(fullDirectoryPath, elementName);
                if (File.Exists(path)) yield return path;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnCommit()
    {
        Log.Debug($"Committing atomic clearing of directory {Path}");

        _pendingFilesDeletes.PopEach(x => File.Delete(x.backupPath));
        _pendingDirectoryDeletes.PopEach(path =>
        {
            if (Directory.Exists(path) && Directory.GetFileSystemEntries(path).Length == 0)
                Directory.Delete(path);
        });
    }

    /// <inheritdoc/>
    protected override void OnRollback()
    {
        Log.Debug($"Rolling back atomic clearing of directory {Path}");

        _pendingFilesDeletes.PopEach(x =>
        {
            try
            {
                File.Move(x.backupPath, x.path);
            }
            #region Error handling
            catch (Exception ex)
            {
                Log.Error($"Failed to roll back clearing of directory {Path}", ex);
                throw;
            }
            #endregion
        });
    }
}
