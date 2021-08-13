// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using NanoByte.Common;
using NanoByte.Common.Storage;
using ZeroInstall.Model;
using ZeroInstall.Store.Manifests;
#if NETFRAMEWORK
using NanoByte.Common.Collections;
#endif

namespace ZeroInstall.Store.Implementations
{
    partial class ImplementationStore
    {
        /// <summary>
        /// Manages state during a single <see cref="IImplementationStore.Optimise"/> run.
        /// </summary>
        private sealed record OptimiseRun(string StorePath) : IDisposable
        {
            private sealed record DedupKey(long Size, long LastModified, ManifestFormat Format, string Digest);

            private sealed record StoreFile(string ImplementationPath, string RelativePath)
            {
                public static implicit operator string(StoreFile file) => System.IO.Path.Combine(file.ImplementationPath, file.RelativePath);
            }

            private readonly Dictionary<DedupKey, StoreFile> _fileHashes = new();
            private readonly HashSet<string> _unsealedImplementations = new();

            /// <summary>
            /// The number of bytes saved by deduplication.
            /// </summary>
            public long SavedBytes;

            /// <summary>
            /// Executes the work-step for a single implementation.
            /// </summary>
            public void Work(ManifestDigest manifestDigest)
            {
                string? digestString = manifestDigest.Best;
                if (digestString == null) return;
                string implementationPath = System.IO.Path.Combine(StorePath, digestString);
                var manifest = Manifest.Load(System.IO.Path.Combine(implementationPath, Manifest.ManifestFile), ManifestFormat.FromPrefix(digestString));

                foreach ((string directoryPath, var directory) in manifest)
                {
                    string currentDirectory = directoryPath.ToNativePath();
                    foreach ((string elementName, var element) in directory)
                    {
                        if (element is ManifestFile(var digest, var modifiedTime, var size) {Size: > 0})
                        {
                            var key = new DedupKey(size, modifiedTime, manifest.Format, digest);
                            var file = new StoreFile(implementationPath, System.IO.Path.Combine(currentDirectory, elementName));

                            if (_fileHashes.TryGetValue(key, out var existingFile))
                            {
                                if (!FileUtils.AreHardlinked(file, existingFile))
                                {
                                    if (JoinWithHardlink(file, existingFile))
                                        SavedBytes += size;
                                }
                            }
                            else _fileHashes.Add(key, file);
                        }
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

                string tempFile = System.IO.Path.Combine(StorePath, System.IO.Path.GetRandomFileName());
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
