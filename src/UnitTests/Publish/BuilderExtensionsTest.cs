// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Net;
using NanoByte.Common.Streams;
using NanoByte.Common.Undo;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Publish;

public class BuilderExtensionsTest
{
    private const string SingleFileData = "data";
    private const string SingleFileName = "file.dat";

    private readonly SimpleCommandExecutor _executor = new();
    private readonly SilentTaskHandler _handler = new();
    private readonly ManifestBuilder _builder = new(ManifestFormat.Sha1New);

    [Fact]
    public void AddArchive()
    {
        using var stream = typeof(RetrievalMethodExtensionsTest).GetEmbeddedStream("testArchive.zip");
        using var microServer = new MicroServer("archive.zip", stream);

        var archive = new Archive {Href = microServer.FileUri};
        _builder.Add(archive, _executor, _handler);

        archive.MimeType.Should().Be(Archive.MimeTypeZip);
        archive.Size.Should().Be(stream.Length);
        _builder.Manifest.Should().NotBeEmpty();
    }

    [Fact]
    public void AddArchiveRelative()
    {
        using var tempDir = new TemporaryDirectory("0install-test");
        typeof(RetrievalMethodExtensionsTest).CopyEmbeddedToFile("testArchive.zip", Path.Combine(tempDir, "archive.zip"));
        _executor.Path = Path.Combine(tempDir, "feed.xml");

        var archive = new Archive {Href = new("archive.zip", UriKind.Relative)};
        _builder.Add(archive, _executor, _handler);

        archive.MimeType.Should().Be(Archive.MimeTypeZip);
        _builder.Manifest.Should().NotBeEmpty();
    }

    [Fact]
    public void AddSingleFile()
    {
        using var stream = SingleFileData.ToStream();
        using var microServer = new MicroServer(SingleFileName, stream);

        var file = new SingleFile {Href = microServer.FileUri, Destination = SingleFileName};
        _builder.Add(file, _executor, _handler);

        file.Size.Should().Be(stream.Length);
        _builder.Manifest.Should().NotBeEmpty();
    }

    [Fact]
    public void AddRecipe()
    {
        using var stream = typeof(RetrievalMethodExtensionsTest).GetEmbeddedStream("testArchive.zip");
        using var microServer = new MicroServer("archive.zip", stream);

        var archive = new Archive {Href = microServer.FileUri};
        var recipe = new Recipe {Steps = {archive}};
        _builder.Add(recipe, _executor, _handler);

        archive.MimeType.Should().Be(Archive.MimeTypeZip);
        archive.Size.Should().Be(stream.Length);
        _builder.Manifest.Should().NotBeEmpty();
    }
}
