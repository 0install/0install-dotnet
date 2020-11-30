// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Store.Implementations.Deployment
{
    /// <summary>
    /// Common base class for testing classes <see cref="DirectoryOperation"/> derived from.
    /// </summary>
    public abstract class DirectoryOperationTestBase : IDisposable
    {
        protected readonly TemporaryDirectory TempDir = new("0install-unit-tests");
        public virtual void Dispose() => TempDir.Dispose();

        protected readonly string File1Path;
        protected readonly string SubdirPath;
        protected readonly string File2Path;
        protected readonly Manifest Manifest;

        protected DirectoryOperationTestBase()
        {
            File1Path = Path.Combine(TempDir, "file1");
            FileUtils.Touch(File1Path);

            SubdirPath = Path.Combine(TempDir, "subdir");
            Directory.CreateDirectory(SubdirPath);

            File2Path = Path.Combine(SubdirPath, "file2");
            FileUtils.Touch(File2Path);

            var generator = new ManifestGenerator(TempDir, ManifestFormat.Sha256New);
            generator.Run();
            Manifest = generator.Manifest;
        }
    }
}
