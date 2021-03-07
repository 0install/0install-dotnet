// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using NanoByte.Common;
using NanoByte.Common.Storage;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Store.Implementations
{
    partial class ImplementationStore
    {
        /// <summary>
        /// Manages state during a single <see cref="IImplementationStore.Optimise"/> run.
        /// </summary>
        private class OptimiseRun : IDisposable
        {
            private sealed record DedupKey(long Size, DateTime LastModified, ManifestFormat Format, string Digest);

            private sealed record StoreFile(string ImplementationPath, string RelativePath)
            {
                public static implicit operator string(StoreFile file) => System.IO.Path.Combine(file.ImplementationPath, file.RelativePath);
            }

            private readonly Dictionary<DedupKey, StoreFile> _fileHashes = new();
            private readonly HashSet<string> _unsealedImplementations = new();
            private readonly string _storePath;

            /// <summary>
            /// The number of bytes saved by deduplication.
            /// </summary>
            public long SavedBytes;

            /// <summary>
            /// Creates a new optimise run.
            /// </summary>
            /// <param name="storePath">The <see cref="IImplementationStore.Path"/>.</param>
            public OptimiseRun(string storePath)
            {
                _storePath = storePath;
            }

            /// <summary>
            /// Executes the work-step for a single implementation.
            /// </summary>
            public void Work(ManifestDigest manifestDigest)
            {
                string? digestString = manifestDigest.Best;
                if (digestString == null) return;
                string implementationPath = System.IO.Path.Combine(_storePath, digestString);
                var manifest = Manifest.Load(System.IO.Path.Combine(implementationPath, Manifest.ManifestFile), ManifestFormat.FromPrefix(digestString));

                string currentDirectory = "";
                foreach (var node in manifest)
                {
                    switch (node)
                    {
                        case ManifestDirectory x:
                            currentDirectory = FileUtils.UnifySlashes(x.FullPath.TrimStart('/'));
                            break;

                        case ManifestFileBase x:
                            if (x.Size == 0) return;

                            var key = new DedupKey(x.Size, x.ModifiedTime, manifest.Format, x.Digest);
                            var file = new StoreFile(implementationPath, System.IO.Path.Combine(currentDirectory, x.Name));

                            if (_fileHashes.TryGetValue(key, out var existingFile))
                            {
                                if (!FileUtils.AreHardlinked(file, existingFile))
                                {
                                    if (JoinWithHardlink(file, existingFile))
                                        SavedBytes += x.Size;
                                }
                            }
                            else _fileHashes.Add(key, file);
                            break;
                    }
                }
            }

            private bool JoinWithHardlink(StoreFile file1, StoreFile file2)
            {
                if (FileUtils.AreHardlinked(file1, file2)) return false;

                if (_unsealedImplementations.Add(file1.ImplementationPath))
                    FileUtils.DisableWriteProtection(file1.ImplementationPath);
                if (_unsealedImplementations.Add(file2.ImplementationPath))
                    FileUtils.DisableWriteProtection(file2.ImplementationPath);

                string tempFile = System.IO.Path.Combine(_storePath, System.IO.Path.GetRandomFileName());
                try
                {
                    Log.Info("Hard link: " + file1 + " <=> " + file2);
                    FileUtils.CreateHardlink(tempFile, file2);
                    FileUtils.Replace(tempFile, file1);
                }
                finally
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }
                return true;
            }

            public void Dispose()
            {
                foreach (string path in _unsealedImplementations)
                    FileUtils.EnableWriteProtection(path);
            }
        }
    }
}
