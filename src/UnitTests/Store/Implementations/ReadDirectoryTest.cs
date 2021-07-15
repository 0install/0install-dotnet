// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using Moq;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using Xunit;
using ZeroInstall.FileSystem;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Store.Implementations
{
    public class ReadDirectoryTest : IDisposable
    {
        private readonly TemporaryDirectory _tempDir = new("0install-test");

        public void Dispose() => _tempDir.Dispose();

        [Fact]
        public void FromManifest()
        {
            new TestRoot
            {
                new TestDirectory("subdir")
                {
                    new TestFile("normal"),
                    new TestFile("executable"),
                    new TestFile("symlink") {Contents = "target"}
                }
            }.Build(_tempDir);

            var manifest = new Manifest(ManifestFormat.Sha1New)
            {
                ["subdir"] =
                {
                    ["normal"] = new ManifestNormalFile("", TestFile.DefaultLastWrite, 0),
                    ["executable"] = new ManifestExecutableFile("", TestFile.DefaultLastWrite, 0),
                    ["symlink"] = new ManifestSymlink("", 0)
                }
            };

            var mock = new Mock<IForwardOnlyImplementationBuilder>();
            new ReadDirectory(_tempDir, mock.Object, manifest).Run();

            mock.Verify(x => x.AddDirectory("subdir"));
            mock.Verify(x => x.AddFile(Path.Combine("subdir", "normal"), It.IsAny<Stream>(), TestFile.DefaultLastWrite, false));
            mock.Verify(x => x.AddFile(Path.Combine("subdir", "executable"), It.IsAny<Stream>(), TestFile.DefaultLastWrite, true));
            mock.Verify(x => x.AddSymlink(Path.Combine("subdir", "symlink"), "target"));
        }

        [Fact]
        public void FromFileSystem()
        {
            new TestRoot
            {
                new TestDirectory("subdir")
                {
                    new TestFile("normal"),
                    new TestFile("executable") {IsExecutable = true},
                    new TestSymlink("symlink", "target")
                }
            }.Build(_tempDir);

            var mock = new Mock<IForwardOnlyImplementationBuilder>();
            new ReadDirectory(_tempDir, mock.Object).Run();

            mock.Verify(x => x.AddDirectory("subdir"));
            mock.Verify(x => x.AddFile(Path.Combine("subdir", "normal"), It.IsAny<Stream>(), TestFile.DefaultLastWrite, false));
            mock.Verify(x => x.AddFile(Path.Combine("subdir", "executable"), It.IsAny<Stream>(), TestFile.DefaultLastWrite, UnixUtils.IsUnix));
            mock.Verify(x => x.AddSymlink(Path.Combine("subdir", "symlink"), "target"));
        }

        [Fact]
        public void DetectHardlinks()
        {
            new TestRoot {new TestFile("a")}.Build(_tempDir);
            FileUtils.CreateHardlink(Path.Combine(_tempDir, "b"), Path.Combine(_tempDir, "a"));

            var mock = new Mock<IForwardOnlyImplementationBuilder>();
            new ReadDirectory(_tempDir, mock.Object).Run();

            mock.Verify(x => x.AddFile("a", It.IsAny<Stream>(), TestFile.DefaultLastWrite, false));
            mock.Verify(x => x.AddHardlink("b", "a", false));

        }
    }
}
