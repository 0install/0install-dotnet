// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Security.Cryptography;
using NanoByte.Common.Net;
using NanoByte.Common.Streams;
using NanoByte.Common.Undo;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Publish;

public class ImplementationExtensionsTest : TestWithMocks
{
    private static readonly ManifestDigest _archiveDigest = new(Sha256New: "TPD62FAK7ME7OCER5CHL3HQDZQMNJVENJUBL6E6IXX5UI44OXMJQ");

    private const string SingleFileData = "data";
    private const string SingleFileName = "file.dat";

    private static readonly string _singleFileSha256Digest = new Manifest(ManifestFormat.Sha256New)
    {
        [""] =
        {
            [SingleFileName] = new ManifestNormalFile(SingleFileData.Hash(SHA256.Create()), 0, SingleFileData.Length)
        }
    }.CalculateDigest();

    [Fact]
    public void SetMissingArchive()
    {
        using var stream = typeof(ImplementationExtensionsTest).GetEmbeddedStream("testArchive.zip");
        using var microServer = new MicroServer("archive.zip", stream);
        var implementation = new Implementation {ID = "1", Version = new("1.0"), RetrievalMethods = {new Archive {Href = microServer.FileUri}}};
        implementation.SetMissing(new SimpleCommandExecutor(), new SilentTaskHandler());
        implementation.ManifestDigest.Should().Be(_archiveDigest);

        var archive = (Archive)implementation.RetrievalMethods[0];
        archive.MimeType.Should().Be(Archive.MimeTypeZip);
        archive.Size.Should().Be(stream.Length);
    }

    [Fact]
    public void SetMissingSingleFile()
    {
        using var originalStream = SingleFileData.ToStream();
        using var microServer = new MicroServer(SingleFileName, originalStream);
        var implementation = new Implementation {ID = "1", Version = new("1.0"), RetrievalMethods = {new SingleFile {Href = microServer.FileUri, Destination = SingleFileName}}};
        implementation.SetMissing(new SimpleCommandExecutor(), new SilentTaskHandler());
        ("sha256new_" + implementation.ManifestDigest.Sha256New).Should().Be(_singleFileSha256Digest);

        var file = (SingleFile)implementation.RetrievalMethods[0];
        file.Size.Should().Be(originalStream.Length);
        file.Destination.Should().Be(SingleFileName);
    }

    [Fact]
    public void SetMissingRecipe()
    {
        using var stream = typeof(ImplementationExtensionsTest).GetEmbeddedStream("testArchive.zip");
        using var microServer = new MicroServer("archive.zip", stream);
        var archive = new Archive {Href = microServer.FileUri};
        var implementation = new Implementation {ID = "1", Version = new("1.0"), RetrievalMethods = {new Recipe {Steps = {archive}}}};
        implementation.SetMissing(new SimpleCommandExecutor(), new SilentTaskHandler());
        implementation.ManifestDigest.Should().Be(_archiveDigest);

        archive.MimeType.Should().Be(Archive.MimeTypeZip);
        archive.Size.Should().Be(stream.Length);
    }

    [Fact]
    public void SetMissingGenerateArchive()
    {
        using var tempDir = new TemporaryDirectory("0install-test-missing");
        string feedPath = Path.Combine(tempDir, "feed.xml");
        Directory.CreateDirectory(Path.Combine(tempDir, "impl"));
        FileUtils.Touch(Path.Combine(tempDir, "impl", "file"));

        var archive = new Archive {Href = new("archive.zip", UriKind.Relative)};
        var implementation = new Implementation {ID = "1", Version = new("1.0"), LocalPath = "impl", RetrievalMethods = {archive}};

        implementation.SetMissing(new SimpleCommandExecutor {Path = feedPath}, new SilentTaskHandler());

        implementation.LocalPath.Should().BeNull();
        implementation.ManifestDigest.Should().NotBe(default(ManifestDigest));
        archive.Size.Should().NotBe(0);

        File.Exists(Path.Combine(tempDir, "archive.zip")).Should().BeTrue();
    }

    [Fact]
    public void SetMissingDigestMismatch()
    {
        using var stream = typeof(ImplementationExtensionsTest).GetEmbeddedStream("testArchive.zip");
        using var microServer = new MicroServer("archive.zip", stream);
        var implementation = new Implementation {ID = "1", Version = new("1.0"), ManifestDigest = new(Sha1New: "invalid"), RetrievalMethods = {new Archive {Href = microServer.FileUri}}};
        Assert.Throws<DigestMismatchException>(() => implementation.SetMissing(new SimpleCommandExecutor(), new SilentTaskHandler()));
    }

    [Fact]
    public void AddArchive()
    {
        using var tempDir = new TemporaryDirectory("0install-test-archive");
        string path = Path.Combine(tempDir, "archive.zip");
        typeof(ImplementationExtensionsTest).CopyEmbeddedToFile("testArchive.zip", path);

        var href = new Uri("http://example.com/archive.zip");
        var implementation = new Implementation {ID = ".", Version = new("1.0")};
        implementation.AddArchive(href, path, extract: null, formats: null, new SimpleCommandExecutor(), new SilentTaskHandler());

        implementation.ManifestDigest.Should().Be(_archiveDigest);
        implementation.ID.Should().Be(_archiveDigest.Best);

        var archive = (Archive)implementation.RetrievalMethods.Should().ContainSingle().Subject;
        archive.Href.Should().Be(href);
        archive.MimeType.Should().Be(Archive.MimeTypeZip);
        archive.Size.Should().Be(new FileInfo(path).Length);
    }

    [Fact]
    public void AddArchiveMultipleFormats()
    {
        using var tempDir = new TemporaryDirectory("0install-test-archive");
        string path = Path.Combine(tempDir, "archive.zip");
        typeof(ImplementationExtensionsTest).CopyEmbeddedToFile("testArchive.zip", path);

        var implementation = new Implementation {ID = ".", Version = new("1.0")};
        implementation.AddArchive(new("http://example.com/archive.zip"), path, extract: null, [ManifestFormat.Sha1New, ManifestFormat.Sha256New], new SimpleCommandExecutor(), new SilentTaskHandler());

        implementation.ManifestDigest.Sha256New.Should().Be(_archiveDigest.Sha256New);
        implementation.ManifestDigest.Sha1New.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AddArchiveMissingLocalFile()
    {
        var implementation = new Implementation {ID = ".", Version = new("1.0")};
        Assert.Throws<FileNotFoundException>(() => implementation.AddArchive(
            new("http://example.com/does-not-exist.zip"), localPath: null, extract: null, formats: null, new SimpleCommandExecutor(), new SilentTaskHandler()));
    }

    [Fact]
    public void AddDigest()
    {
        using var tempDir = new TemporaryDirectory("0install-test-digest");
        FileUtils.Touch(Path.Combine(tempDir, "file"));

        var digest = new ManifestDigest(BuildDigest(tempDir, ManifestFormat.Sha256New));
        var implementation = new Implementation {ID = digest.Best!, Version = new("1.0"), ManifestDigest = digest};

        var storeMock = GetMock<IImplementationStore>();
        storeMock.Setup(x => x.GetPath(digest)).Returns(tempDir.Path);
        storeMock.Setup(x => x.Verify(digest));

        implementation.AddDigest(ManifestFormat.Sha1New, storeMock.Object, new SimpleCommandExecutor(), new SilentTaskHandler())
                      .Should().BeTrue();

        implementation.ManifestDigest.Sha256New.Should().Be(digest.Sha256New, because: "Existing digests should be preserved");
        ("sha1new=" + implementation.ManifestDigest.Sha1New).Should().Be(BuildDigest(tempDir, ManifestFormat.Sha1New));
    }

    [Fact]
    public void AddDigestSkipsExistingFormat()
    {
        var implementation = new Implementation {ID = "sha1new=abc", Version = new("1.0"), ManifestDigest = new(Sha1New: "abc")};

        implementation.AddDigest(ManifestFormat.Sha1New, GetMock<IImplementationStore>().Object, new SimpleCommandExecutor(), new SilentTaskHandler())
                      .Should().BeFalse();
    }

    [Fact]
    public void AddDigestSkipsUncachedImplementation()
    {
        var digest = new ManifestDigest(Sha256New: "TPD62FAK7ME7OCER5CHL3HQDZQMNJVENJUBL6E6IXX5UI44OXMJQ");
        var implementation = new Implementation {ID = digest.Best!, Version = new("1.0"), ManifestDigest = digest};

        var storeMock = GetMock<IImplementationStore>();
        storeMock.Setup(x => x.GetPath(digest)).Returns(() => null);

        implementation.AddDigest(ManifestFormat.Sha1New, storeMock.Object, new SimpleCommandExecutor(), new SilentTaskHandler())
                      .Should().BeFalse();
    }

    private static string BuildDigest(string path, ManifestFormat format)
    {
        var builder = new ManifestBuilder(format);
        new SilentTaskHandler().RunTask(new ReadDirectory(path, builder));
        return builder.Manifest.CalculateDigest();
    }
}
