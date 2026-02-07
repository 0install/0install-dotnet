// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.FileSystem;

public class ReadDirectoryTest : IDisposable
{
    private readonly TemporaryDirectory _tempDir = new("0install-test");

    public void Dispose() => _tempDir.Dispose();

    [Fact]
    public void FromFileSystem()
    {
        new TestRoot
        {
            new TestDirectory("subdir")
            {
                new TestFile("normal"),
                new TestFile("executable") {IsExecutable = true},
                new TestSymlink("symlink", "normal")
            }
        }.Build(_tempDir);

        var mock = new Mock<IForwardOnlyBuilder>();
        new ReadDirectory(_tempDir, mock.Object).Run();

        mock.Verify(x => x.AddDirectory("subdir"));
        mock.Verify(x => x.AddFile(Path.Combine("subdir", "normal"), It.IsAny<Stream>(), TestFile.DefaultLastWrite, false));
        mock.Verify(x => x.AddFile(Path.Combine("subdir", "executable"), It.IsAny<Stream>(), TestFile.DefaultLastWrite, true));
        mock.Verify(x => x.AddSymlink(Path.Combine("subdir", "symlink"), "normal"));
    }

    [SkippableFact]
    public void FromManifest()
    {
        Skip.If(UnixUtils.IsUnix, "Manifest file is only used to detect executable bits and symlinks on non-Unix file systems");

        new TestRoot
        {
            new TestDirectory("subdir")
            {
                new TestFile("normal") {IsExecutable = true}, // This executable bit should be ignored because the manifest file takes precedence
                new TestFile("executable"),
                new TestFile("symlink") {Contents = "normal"}
            }
        }.Build(_tempDir);

        new Manifest(ManifestFormat.Sha1New)
        {
            ["subdir"] =
            {
                ["normal"] = new ManifestNormalFile("", TestFile.DefaultLastWrite, 0),
                ["executable"] = new ManifestExecutableFile("", TestFile.DefaultLastWrite, 0),
                ["symlink"] = new ManifestSymlink("", 0)
            }
        }.Save(Path.Combine(_tempDir, Manifest.ManifestFile));

        var mock = new Mock<IForwardOnlyBuilder>();
        new ReadDirectory(_tempDir, mock.Object).Run();

        mock.Verify(x => x.AddDirectory("subdir"));
        mock.Verify(x => x.AddFile(Path.Combine("subdir", "normal"), It.IsAny<Stream>(), TestFile.DefaultLastWrite, false));
        mock.Verify(x => x.AddFile(Path.Combine("subdir", "executable"), It.IsAny<Stream>(), TestFile.DefaultLastWrite, true));
        mock.Verify(x => x.AddSymlink(Path.Combine("subdir", "symlink"), "normal"));
    }

    [Fact]
    public void DetectHardlinks()
    {
        new TestRoot {new TestFile("a")}.Build(_tempDir);
        FileUtils.CreateHardlink(Path.Combine(_tempDir, "b"), Path.Combine(_tempDir, "a"));

        var mock = new Mock<IForwardOnlyBuilder>();
        new ReadDirectory(_tempDir, mock.Object).Run();

        mock.Verify(x => x.AddFile("a", It.IsAny<Stream>(), TestFile.DefaultLastWrite, false));
        mock.Verify(x => x.AddHardlink("b", "a", false));
    }

    [Fact]
    public void SubDir()
    {
        new TestRoot
        {
            new TestFile("root-file"),
            new TestDirectory("subdir")
            {
                new TestFile("sub-file") { IsExecutable = true }
            }
        }.Build(_tempDir);

        var mock = new Mock<IForwardOnlyBuilder>();
        new ReadDirectory(_tempDir, mock.Object, subDir: "subdir").Run();

        mock.Verify(x => x.AddFile("sub-file", It.IsAny<Stream>(), TestFile.DefaultLastWrite, true));
        mock.VerifyNoOtherCalls();
    }
}
