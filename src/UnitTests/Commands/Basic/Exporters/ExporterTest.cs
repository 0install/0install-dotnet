// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Icons;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Commands.Basic.Exporters;

public class ExporterTest : TestWithMocks
{
    private readonly TemporaryDirectory _destination;
    private readonly Exporter _target;

    public ExporterTest()
    {
        _destination = new TemporaryDirectory("0install-unit-test");
        var selections = Fake.Selections;
        _target = new Exporter(selections, new Architecture(), _destination);
    }

    public override void Dispose()
    {
        base.Dispose();
        _destination.Dispose();
    }

    [Fact]
    public void ExportFeeds()
    {
        using var feedFile1 = new TemporaryFile("0install-test-feed");
        using var subFeedFile1 = new TemporaryFile("0install-test-feed");
        using var feedFile2 = new TemporaryFile("0install-test-feed");
        using var subFeedFile2 = new TemporaryFile("0install-test-feed");
        var feedCacheMock = GetMock<IFeedCache>();

        feedCacheMock.Setup(x => x.GetPath(Fake.Feed1Uri)).Returns(feedFile1);
        feedCacheMock.Setup(x => x.GetPath(Fake.SubFeed1Uri)).Returns(subFeedFile1);
        feedCacheMock.Setup(x => x.GetPath(Fake.Feed2Uri)).Returns(feedFile2);
        feedCacheMock.Setup(x => x.GetPath(Fake.SubFeed2Uri)).Returns(subFeedFile2);

        var signature = new ValidSignature(123, Array.Empty<byte>(), new DateTime(2000, 1, 1));
        feedCacheMock.Setup(x => x.GetSignatures(Fake.Feed1Uri)).Returns(new OpenPgpSignature[] {signature});
        feedCacheMock.Setup(x => x.GetSignatures(Fake.SubFeed1Uri)).Returns(new OpenPgpSignature[] {signature});
        feedCacheMock.Setup(x => x.GetSignatures(Fake.Feed2Uri)).Returns(new OpenPgpSignature[] {signature});
        feedCacheMock.Setup(x => x.GetSignatures(Fake.SubFeed2Uri)).Returns(new OpenPgpSignature[] {signature});

        var openPgpMock = GetMock<IOpenPgp>();
        openPgpMock.Setup(x => x.ExportKey(signature)).Returns("abc");

        _target.ExportFeeds(feedCacheMock.Object, openPgpMock.Object);

        string contentDir = Path.Combine(_destination, "content");
        File.Exists(Path.Combine(contentDir, Fake.Feed1Uri.Escape())).Should().BeTrue();
        File.Exists(Path.Combine(contentDir, Fake.SubFeed1Uri.Escape())).Should().BeTrue();
        File.Exists(Path.Combine(contentDir, Fake.Feed2Uri.Escape())).Should().BeTrue();
        File.Exists(Path.Combine(contentDir, Fake.SubFeed2Uri.Escape())).Should().BeTrue();

        File.ReadAllText(Path.Combine(contentDir, "000000000000007B.gpg")).Should()
            .Be("abc", because: "GPG keys should be exported.");
    }

    [Fact]
    public void ExportImplementations()
    {
        using (var implDir1 = new TemporaryDirectory("0install-test-impl"))
        using (var implDir2 = new TemporaryDirectory("0install-test-impl"))
        {
            var storeMock = GetMock<IImplementationStore>();
            storeMock.Setup(x => x.GetPath(new ManifestDigest(null, null, "123", null))).Returns(implDir1);
            storeMock.Setup(x => x.GetPath(new ManifestDigest(null, null, "abc", null))).Returns(implDir2);

            _target.ExportImplementations(storeMock.Object, new SilentTaskHandler());
        }

        string contentDir = Path.Combine(_destination, "content");
        File.Exists(Path.Combine(contentDir, "sha256=123.tgz")).Should()
            .BeTrue(because: "Implementation should be exported.");
        File.Exists(Path.Combine(contentDir, "sha256=abc.tgz")).Should()
            .BeTrue(because: "Implementation should be exported.");
    }

    [Fact]
    public void ExportIcons()
    {
        var icon = new Icon {Href = new("https://example.com/myicon"), MimeType = Icon.MimeTypePng};

        using var tempFile = new TemporaryFile("0install-unit-tests");
        var iconStoreMock = new Mock<IIconStore>();
        iconStoreMock.Setup(x => x.GetFresh(icon))
                     .Returns(tempFile);

        _target.ExportIcons(new[] {icon}, iconStoreMock.Object);

        string contentDir = Path.Combine(_destination, "content");
        File.Exists(Path.Combine(contentDir, "https%3a%2f%2fexample.com%2fmyicon.png")).Should()
            .BeTrue(because: "Icon should be exported.");
    }
}
