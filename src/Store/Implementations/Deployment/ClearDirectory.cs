// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Implementations.Manifests;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Deployment
{
    /// <summary>
    /// Deletes files listed in a <see cref="Manifest"/> file from a directory.
    /// </summary>
    public class ClearDirectory : DirectoryOperation
    {
        private readonly Stack<string> _pendingDirectoryDeletes = new();

        /// <summary>
        /// Creates a new directory clear task.
        /// </summary>
        /// <param name="path">The path of the directory to clear.</param>
        /// <param name="manifest">The contents of a <see cref="Manifest"/> file describing the directory.</param>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about IO tasks.</param>
        public ClearDirectory(string path, Manifest manifest, ITaskHandler handler)
            : base(path, manifest, handler)
        {}

        private readonly Stack<(string path, string backupPath)> _pendingFilesDeletes = new();

        /// <inheritdoc/>
        protected override void OnStage()
        {
            Log.Debug($"Preparing atomic clearing of directory {Path}");

            _pendingDirectoryDeletes.Push(Path);

            var filesToDelete = new List<string>();

            string manifestPath = System.IO.Path.Combine(Path, Manifest.ManifestFile);
            if (File.Exists(manifestPath))
                filesToDelete.Add(manifestPath);

            foreach ((string relativePath, var node) in ElementPaths)
            {
                string elementPath = System.IO.Path.Combine(Path, relativePath);

                if (node is ManifestDirectory)
                {
                    if (Directory.Exists(elementPath))
                        _pendingDirectoryDeletes.Push(elementPath);
                }
                else
                {
                    if (File.Exists(elementPath))
                        filesToDelete.Add(elementPath);
                }
            }

            if (filesToDelete.Count != 0)
            {
                UnlockFiles(filesToDelete);

                Handler.RunTask(ForEachTask.Create(Resources.DeletingObsoleteFiles, filesToDelete, path =>
                {
                    string tempPath = Randomize(path);
                    File.Move(path, tempPath);
                    _pendingFilesDeletes.Push((path, tempPath));
                }));
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
                    Log.Error(ex);
                    throw;
                }
                #endregion
            });
        }
    }
}
