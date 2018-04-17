// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
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
        private readonly Stack<string> _pendingDirectoryDeletes = new Stack<string>();

        /// <summary>
        /// Creates a new directory clear task.
        /// </summary>
        /// <param name="path">The path of the directory to clear.</param>
        /// <param name="manifest">The contents of a <see cref="Manifest"/> file describing the directory.</param>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about IO tasks.</param>
        public ClearDirectory([NotNull] string path, [NotNull] Manifest manifest, [NotNull] ITaskHandler handler)
            : base(path, manifest, handler)
        {}

        private readonly Stack<KeyValuePair<string, string>> _pendingFilesDeletes = new Stack<KeyValuePair<string, string>>();

        /// <inheritdoc/>
        protected override void OnStage()
        {
            _pendingDirectoryDeletes.Push(Path);

            var filesToDelete = new List<string>();

            string manifestPath = System.IO.Path.Combine(Path, Manifest.ManifestFile);
            if (File.Exists(manifestPath))
                filesToDelete.Add(manifestPath);

            foreach (var pair in ElementPaths)
            {
                string elementPath = System.IO.Path.Combine(Path, pair.Key);

                if (pair.Value is ManifestDirectory)
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
                    _pendingFilesDeletes.Push(new KeyValuePair<string, string>(path, tempPath));
                }));
            }
        }

        /// <inheritdoc/>
        protected override void OnCommit()
            => Handler.RunTask(new SimpleTask(Resources.DeletingObsoleteFiles, () =>
            {
                _pendingFilesDeletes.PopEach(x => File.Delete(x.Value));
                _pendingDirectoryDeletes.PopEach(path =>
                {
                    if (Directory.Exists(path) && Directory.GetFileSystemEntries(path).Length == 0)
                        Directory.Delete(path);
                });
            }));

        /// <inheritdoc/>
        protected override void OnRollback()
        {
            _pendingFilesDeletes.PopEach(x =>
            {
                try
                {
                    File.Move(x.Value, x.Key);
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
