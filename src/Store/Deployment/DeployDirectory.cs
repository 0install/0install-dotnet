// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Deployment
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
        private readonly Stack<(string source, string destination)> _pendingFileRenames = new();

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

            string manifestPath = System.IO.Path.Combine(DestinationPath, Manifest.ManifestFile);
            string tempManifestPath = Randomize(manifestPath);
            _pendingFileRenames.Push((tempManifestPath, manifestPath));
            Manifest.Save(tempManifestPath);

            Handler.RunTask(new SimpleTask(Resources.CopyFiles, () =>
            {
                foreach ((string directoryPath, var directory) in Manifest)
                {
                    string dirPath = directoryPath.ToNativePath();
                    string sourceDir = System.IO.Path.Combine(Path, dirPath);
                    string destinationDir = System.IO.Path.Combine(DestinationPath, dirPath);
                    if (!Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                        _createdDirectories.Push(destinationDir);
                    }

                    foreach ((string path, var element) in directory)
                    {
                        string sourcePath = System.IO.Path.Combine(sourceDir, path);
                        string destinationPath = System.IO.Path.Combine(destinationDir, path);

                        string tempPath = Randomize(destinationPath);
                        _pendingFileRenames.Push((tempPath, destinationPath));

                        switch (element)
                        {
                            case ManifestFile file:
                                File.Copy(sourcePath, tempPath);
                                File.SetLastWriteTimeUtc(tempPath, file.ModifiedTime);

                                if (file is ManifestExecutableFile)
                                    ImplFileUtils.SetExecutable(tempPath);
                                break;

                            case ManifestSymlink:
                                if (ImplFileUtils.IsSymlink(sourcePath, out string? symlinkTarget))
                                    ImplFileUtils.CreateSymlink(tempPath, symlinkTarget);
                                break;
                        }
                    }
                }
            }));
        }

        /// <inheritdoc/>
        protected override void OnCommit()
        {
            Log.Debug($"Committing atomic deployment to {DestinationPath}");

            UnlockFiles(_pendingFileRenames.Select(x => x.destination).Where(File.Exists));

            _pendingFileRenames.PopEach(x =>
            {
                if (File.Exists(x.destination))
                    File.Delete(x.destination);
                File.Move(x.source, x.destination);
            });
        }

        /// <inheritdoc/>
        protected override void OnRollback()
        {
            Log.Debug($"Rolling back atomic deployment to {DestinationPath}");

            _pendingFileRenames.PopEach(x =>
            {
                if (File.Exists(x.source))
                    File.Delete(x.source);
            });

            _createdDirectories.PopEach(path =>
            {
                if (Directory.Exists(path) && Directory.GetFileSystemEntries(path).Length == 0)
                    Directory.Delete(path);
            });
        }
    }
}
