// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;
using NanoByte.Common.Undo;

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

    [Fact]
    public void CalculateDigestMultipleFormats()
    {
        using var sourceDir = new TemporaryDirectory("0install-test-digest");
        string localFile = Path.Combine(sourceDir, "archive.zip");
        typeof(RetrievalMethodExtensionsTest).CopyEmbeddedToFile("testArchive.zip", localFile);

        var archive = new Archive {Href = new Uri(localFile)};
        var existingDigest = new ManifestDigest(Sha1New: "", Sha256: "", Sha256New: "");
        
        var digest = archive.CalculateDigest(new SimpleCommandExecutor(), _handler, existingDigest);

        // All requested formats should be populated
        digest.Sha1New.Should().NotBeNullOrEmpty();
        digest.Sha256.Should().NotBeNullOrEmpty();
        digest.Sha256New.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CalculateDigestPartialFormats()
    {
        using var sourceDir = new TemporaryDirectory("0install-test-digest-partial");
        string localFile = Path.Combine(sourceDir, "archive.zip");
        typeof(RetrievalMethodExtensionsTest).CopyEmbeddedToFile("testArchive.zip", localFile);

        var archive = new Archive {Href = new Uri(localFile)};
        var existingDigest = new ManifestDigest(Sha1New: "", Sha256New: "");
        
        var digest = archive.CalculateDigest(new SimpleCommandExecutor(), _handler, existingDigest);

        // Only requested formats should be populated
        digest.Sha1New.Should().NotBeNullOrEmpty();
        digest.Sha256.Should().BeNullOrEmpty();
        digest.Sha256New.Should().NotBeNullOrEmpty();
    }
}
