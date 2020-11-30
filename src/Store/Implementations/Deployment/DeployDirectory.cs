// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NanoByte.Common.Collections;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Implementations.Manifests;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Deployment
{
    /// <summary>
    /// Deploys/copies files listed in a <see cref="Manifest"/> file to another directory.
    /// </summary>
    public class DeployDirectory : DirectoryOperation
    {
        /// <summary>
        /// The path of the destination directory. May already exist.
        /// </summary>
        public string DestinationPath { get; }

        /// <summary>
        /// Creates a new directory deployment task.
        /// </summary>
        /// <param name="sourcePath">The path of the source directory to copy from.</param>
        /// <param name="sourceManifest">The contents of a <see cref="Manifest"/> file describing the source directory.</param>
        /// <param name="destinationPath">The path of the destination directory to copy to.</param>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about IO tasks.</param>
        public DeployDirectory(string sourcePath, Manifest sourceManifest, string destinationPath, ITaskHandler handler)
            : base(sourcePath, sourceManifest, handler)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));
            #endregion

            DestinationPath = destinationPath;
        }

        private readonly Stack<string> _createdDirectories = new();
        private readonly Stack<KeyValuePair<string, string>> _pendingFileRenames = new();

        /// <inheritdoc/>
        protected override void OnStage()
        {
            if (!Directory.Exists(DestinationPath))
            {
                Directory.CreateDirectory(DestinationPath);
                _createdDirectories.Push(DestinationPath);
            }

            if (FileUtils.DetermineTimeAccuracy(DestinationPath) > 0)
                throw new IOException(Resources.InsufficientFSTimeAccuracy);

            string manifestPath = System.IO.Path.Combine(DestinationPath, Manifest.ManifestFile);
            string tempManifestPath = Randomize(manifestPath);
            _pendingFileRenames.Push(new KeyValuePair<string, string>(tempManifestPath, manifestPath));
            Manifest.Save(tempManifestPath);

            Handler.RunTask(ForEachTask.Create(Resources.CopyFiles, ElementPaths, pair =>
            {
                string sourcePath = System.IO.Path.Combine(Path, pair.Key);
                string destinationPath = System.IO.Path.Combine(DestinationPath, pair.Key);

                if (pair.Value is ManifestDirectory)
                {
                    if (!Directory.Exists(destinationPath))
                    {
                        Directory.CreateDirectory(destinationPath);
                        _createdDirectories.Push(destinationPath);
                    }
                }
                else
                {
                    string tempPath = Randomize(destinationPath);
                    _pendingFileRenames.Push(new KeyValuePair<string, string>(tempPath, destinationPath));

                    switch (pair.Value)
                    {
                        case ManifestFileBase file:
                            File.Copy(sourcePath, tempPath);
                            File.SetLastWriteTimeUtc(tempPath, file.ModifiedTime);

                            if (UnixUtils.IsUnix)
                                FileUtils.SetExecutable(tempPath, file is ManifestExecutableFile);
                            break;

                        case ManifestSymlink _:
                            if (UnixUtils.IsUnix)
                            {
                                if (UnixUtils.IsSymlink(sourcePath, out string? symlinkTarget))
                                    UnixUtils.CreateSymlink(tempPath, symlinkTarget);
                            }
                            break;
                    }
                }
            }));
        }

        /// <inheritdoc/>
        protected override void OnCommit()
        {
            UnlockFiles(_pendingFileRenames.Select(x => x.Value).Where(File.Exists));

            Handler.RunTask(new SimpleTask(Resources.CopyFiles, () =>
            {
                _pendingFileRenames.PopEach(x =>
                {
                    if (File.Exists(x.Value))
                        File.Delete(x.Value);
                    File.Move(x.Key, x.Value);
                });
            }));
        }

        /// <inheritdoc/>
        protected override void OnRollback()
        {
            _pendingFileRenames.PopEach(x =>
            {
                if (File.Exists(x.Key))
                    File.Delete(x.Key);
            });

            _createdDirectories.PopEach(path =>
            {
                if (Directory.Exists(path) && Directory.GetFileSystemEntries(path).Length == 0)
                    Directory.Delete(path);
            });
        }
    }
}
