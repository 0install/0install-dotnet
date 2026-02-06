// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.FileSystem;

public class ReadDirectoryAsHardlinksTest : IDisposable
{
    private readonly TemporaryDirectory _tempDir = new("0install-test");
    private readonly TemporaryDirectory _sourceDir = new("0install-test-source");

    public void Dispose()
    {
        _tempDir.Dispose();
        _sourceDir.Dispose();
    }

    [Fact]
    public void CreatesHardlinks()
    {
        // Create source directory with files
        new TestRoot
        {
            new TestDirectory("subdir")
            {
                new TestFile("normal"),
                new TestFile("executable") {IsExecutable = true}
            }
        }.Build(_sourceDir);

        // Create a mock builder
        var mock = new Mock<IForwardOnlyBuilder>();
        new ReadDirectoryAsHardlinks(_sourceDir, mock.Object, _sourceDir, "Test").Run();

        // Verify that directories are created
        mock.Verify(x => x.AddDirectory("subdir"));
        
        // Verify that hardlinks are created instead of files
        mock.Verify(x => x.AddHardlink(Path.Combine("subdir", "normal"), It.IsAny<string>(), false), Times.Once);
        mock.Verify(x => x.AddHardlink(Path.Combine("subdir", "executable"), It.IsAny<string>(), true), Times.Once);
        
        // Verify that regular AddFile is not called
        mock.Verify(x => x.AddFile(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<UnixTime>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void HandlesSymlinks()
    {
        new TestRoot
        {
            new TestDirectory("subdir")
            {
                new TestFile("normal"),
                new TestSymlink("symlink", "normal")
            }
        }.Build(_sourceDir);

        var mock = new Mock<IForwardOnlyBuilder>();
        new ReadDirectoryAsHardlinks(_sourceDir, mock.Object, _sourceDir, "Test").Run();

        mock.Verify(x => x.AddDirectory("subdir"));
        mock.Verify(x => x.AddHardlink(Path.Combine("subdir", "normal"), It.IsAny<string>(), false), Times.Once);
        mock.Verify(x => x.AddSymlink(Path.Combine("subdir", "symlink"), "normal"), Times.Once);
    }

    [Fact]
    public void FallbackToFileWhenHardlinkNotSupported()
    {
        new TestRoot
        {
            new TestFile("test")
        }.Build(_sourceDir);

        // Create a mock that throws NotSupportedException for AddHardlink
        var mock = new Mock<IForwardOnlyBuilder>();
        mock.Setup(x => x.AddHardlink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Throws<NotSupportedException>();

        new ReadDirectoryAsHardlinks(_sourceDir, mock.Object, _sourceDir, "Test").Run();

        // Should fall back to AddFile
        mock.Verify(x => x.AddFile("test", It.IsAny<Stream>(), TestFile.DefaultLastWrite, false), Times.Once);
    }
}
