// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Publish.EntryPoints;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish;

/// <summary>
/// Contains test methods for <see cref="FeedBuilder"/>.
/// </summary>
public class FeedBuilderTest : IDisposable
{
    private readonly FeedBuilder _builder = new();
    private readonly TemporaryDirectory _implementationDir = new("0install-test-impl");

    public void Dispose()
    {
        _builder.Dispose();
        _implementationDir.Dispose();
    }

    [Fact]
    public void GenerateDigest()
    {
        _builder.ImplementationDirectory = _implementationDir;
        _builder.GenerateDigest(new SilentTaskHandler());
        _builder.ID.Should().Be(ManifestDigest.Empty.Best);
        _builder.ManifestDigest.PartialEquals(ManifestDigest.Empty).Should().BeTrue();
    }

    [Fact]
    public void DetectCandidates()
    {
        _builder.ImplementationDirectory = _implementationDir;
        _builder.DetectCandidates(new SilentTaskHandler());
    }

    [Fact]
    public void GenerateCommands()
    {
        _builder.MainCandidate = new WindowsExe
        {
            RelativePath = "test",
            Name = "TestApp",
            Summary = "a test app",
            Version = new("1.0"),
            Architecture = new(OS.Windows)
        };
        _builder.GenerateCommands();
    }

    [Fact]
    public void Build()
    {
        GenerateDigest();
        DetectCandidates();
        GenerateCommands();

        _builder.RetrievalMethod = new Archive {Href = new("http://example.com/archive.zip")};
        _builder.Uri = new("http://example.com/test1.xml");
        _builder.Icons.Add(new Icon {MimeType = Icon.MimeTypePng, Href = new("http://example.com/test.png")});
        _builder.Icons.Add(new Icon {MimeType = Icon.MimeTypeIco, Href = new("http://example.com/test.ico")});
        _builder.SecretKey = new OpenPgpSecretKey(keyID: 123, fingerprint: new byte[] {1, 2, 3}, userID: "user");
        var signedFeed = _builder.Build();

        signedFeed.Feed.Name.Should().Be(_builder.MainCandidate.Name);
        signedFeed.Feed.Uri.Should().Be(_builder.Uri);
        signedFeed.Feed.Summaries.Should().Equal(new LocalizableStringCollection {_builder.MainCandidate.Summary});
        signedFeed.Feed.NeedsTerminal.Should().Be(_builder.MainCandidate.NeedsTerminal);
        signedFeed.Feed.Elements.Should().Equal(
            new Implementation
            {
                ID = ManifestDigest.Empty.Best,
                ManifestDigest = new(Sha256New: ManifestDigest.Empty.Sha256New),
                Version = _builder.MainCandidate.Version,
                Architecture = _builder.MainCandidate.Architecture,
                Commands = {new() {Name = Command.NameRun, Path = "test"}},
                RetrievalMethods = {_builder.RetrievalMethod}
            });
        signedFeed.Feed.Icons.Should().Equal(_builder.Icons);
        signedFeed.SecretKey.Should().Be(_builder.SecretKey);
    }

    [Fact]
    public void TemporaryDirectory()
    {
        var tempDir1 = new TemporaryDirectory("0install-test-temp");
        var tempDir2 = new TemporaryDirectory("0install-test-temp");

        _builder.TemporaryDirectory = tempDir1;
        Directory.Exists(tempDir1).Should().BeTrue(because: "Directory should exist");

        _builder.TemporaryDirectory = tempDir2;
        Directory.Exists(tempDir1).Should().BeFalse(because: "Directory should be auto-disposed when replaced with a new one");
        Directory.Exists(tempDir2).Should().BeTrue(because: "Directory should exist");

        _builder.Dispose();
        Directory.Exists(tempDir2).Should().BeFalse(because: "Directory should be disposed together with FeedBuilder");
    }
}
