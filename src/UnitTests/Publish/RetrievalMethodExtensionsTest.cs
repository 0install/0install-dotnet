// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;

namespace ZeroInstall.Publish;

public class RetrievalMethodExtensionsTest
{
    private readonly SilentTaskHandler _handler = new();

    [Fact]
    public void ToTempDirArchive()
    {
        using var sourceDir = new TemporaryDirectory("0install-test-archive");
        string localFile = Path.Combine(sourceDir, "archive.zip");
        typeof(RetrievalMethodExtensionsTest).CopyEmbeddedToFile("testArchive.zip", localFile);

        var archive = new Archive {Href = null!};
        using (var tempDir = archive.ToTempDir(_handler, localFile))
            File.Exists(Path.Combine(tempDir, "symlink")).Should().BeTrue();

        archive.MimeType.Should().Be(Archive.MimeTypeZip);
        archive.Size.Should().Be(new FileInfo(localFile).Length);
    }

    [Fact]
    public void ToTempDirSingleFile()
    {
        using var sourceDir = new TemporaryDirectory("0install-test-file");
        string localFile = Path.Combine(sourceDir, "file");
        File.WriteAllText(localFile, @"abc");

        var file = new SingleFile {Href = null!, Destination = null!};
        using (var tempDir = file.ToTempDir(_handler, localFile))
            File.Exists(Path.Combine(tempDir, "file")).Should().BeTrue();

        file.Destination.Should().Be("file");
        file.Size.Should().Be(3);
    }
}
