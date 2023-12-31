// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Net;
using NanoByte.Common.Streams;
using ZeroInstall.Model.Preferences;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds;

/// <summary>
/// Contains test methods for <see cref="FeedManager"/>.
/// </summary>
public class FeedManagerTest : TestWithMocksAndRedirect
{
    private readonly Config _config = new();
    private readonly Mock<IFeedCache> _feedCacheMock;
    private readonly Mock<ITrustManager> _trustManagerMock;
    private readonly IFeedManager _feedManager;

    private readonly Feed _feedPreNormalize;
    private readonly Feed _feedPostNormalize;

    public FeedManagerTest()
    {
        _feedCacheMock = GetMock<IFeedCache>();
        _trustManagerMock = GetMock<ITrustManager>();
        _feedManager = new FeedManager(_config, _feedCacheMock.Object, _trustManagerMock.Object, new SilentTaskHandler());

        _feedPreNormalize = FeedTest.CreateTestFeed();

        _feedPostNormalize = _feedPreNormalize.Clone();
        _feedPostNormalize.Normalize(_feedPreNormalize.Uri);
    }

    [Fact]
    public void Local()
    {
        using var feedFile = new TemporaryFile("0install-test-feed");
        _feedPreNormalize.SaveXml(feedFile);

        var result = _feedManager[new FeedUri(feedFile)];
        result.Should().Be(_feedPostNormalize);
        _feedManager.Stale.Should().BeFalse();
    }

    [Fact]
    public void LocalMissing()
    {
        using var tempDir = new TemporaryDirectory("0install-test-missing");
        Assert.Throws<FileNotFoundException>(() => _feedManager[new FeedUri(Path.Combine(tempDir, "invalid"))]);
    }

    [Fact]
    public void Download()
    {
        var feed = FeedTest.CreateTestFeed();
        var feedStream = new MemoryStream();
        using var server = new MicroServer("feed.xml", feedStream);
        feed.Uri = new(server.FileUri);
        feed.SaveXml(feedStream);
        var data = feedStream.ToArray();
        feedStream.Position = 0;

        _feedManager.IsStale(feed.Uri).Should().BeTrue(because: "Non-cached feeds should be reported as stale");

        // No previous feed
        var sequence =
            _feedCacheMock.SetupSequence(x => x.GetFeed(feed.Uri))
                          .Returns((Feed?)null);
        _feedCacheMock.Setup(x => x.GetSignatures(feed.Uri))
                      .Returns([]);

        // Adding new feed
        _feedCacheMock.Setup(x => x.Add(feed.Uri, data));
        sequence.Returns(feed);
        _trustManagerMock.Setup(x => x.CheckTrust(data, feed.Uri, It.IsAny<OpenPgpKeyCallback>())).Returns(OpenPgpUtilsTest.TestSignature);

        _feedManager[feed.Uri].Should().Be(feed);
    }

    [Fact]
    public void DownloadIncorrectUri()
    {
        var feed = FeedTest.CreateTestFeed();
        var feedStream = new MemoryStream();
        feed.SaveXml(feedStream);
        feedStream.Position = 0;

        using var server = new MicroServer("feed.xml", feedStream);
        var feedUri = new FeedUri(server.FileUri);

        // No previous feed
        _feedCacheMock.Setup(x => x.GetFeed(feedUri))
                      .Returns((Feed?)null);

        Assert.Throws<InvalidDataException>(() => _feedManager[feedUri]);
    }

    [Fact]
    public void DownloadFromMirror()
    {
        var feed = FeedTest.CreateTestFeed();
        feed.Uri = new("http://invalid/directory/feed.xml");
        var data = feed.ToXmlString().ToStream().ToArray();

        // No previous feed
        var sequence =
            _feedCacheMock.SetupSequence(x => x.GetFeed(feed.Uri))
                          .Returns((Feed?)null);
        _feedCacheMock.Setup(x => x.GetSignatures(feed.Uri))
                      .Returns([]);


        // Adding new feed
        _feedCacheMock.Setup(x => x.Add(feed.Uri, data));
        sequence.Returns(feed);
        _trustManagerMock.Setup(x => x.CheckTrust(data, feed.Uri, It.IsAny<OpenPgpKeyCallback>())).Returns(OpenPgpUtilsTest.TestSignature);

        using var mirrorServer = new MicroServer("feeds/http/invalid/directory%23feed.xml/latest.xml", new MemoryStream(data));
        _config.FeedMirror = new(mirrorServer.ServerUri);
        _feedManager[feed.Uri].Should().Be(feed);
    }

    [Fact]
    public void DetectFreshCached()
    {
        _feedCacheMock.Setup(x => x.GetFeed(FeedTest.Test1Uri)).Returns(_feedPreNormalize);
        new FeedPreferences {LastChecked = DateTime.UtcNow}.SaveFor(FeedTest.Test1Uri);

        _feedManager.IsStale(FeedTest.Test1Uri).Should().BeFalse();
        _feedManager[FeedTest.Test1Uri].Should().Be(_feedPostNormalize);
        _feedManager.Stale.Should().BeFalse();
    }

    [Fact]
    public void ServeFromInMemoryCache()
    {
        DetectFreshCached();

        // ReSharper disable once UnusedVariable
        var _ = _feedManager[FeedTest.Test1Uri];

        _feedCacheMock.Verify(x => x.GetFeed(FeedTest.Test1Uri), Times.Once(),
            failMessage: "Underlying cache was accessed more than once instead of being handled by the in-memory cache.");
    }

    [Fact]
    public void Refresh()
    {
        var feed = FeedTest.CreateTestFeed();
        var feedStream = new MemoryStream();
        using var server = new MicroServer("feed.xml", feedStream);
        feed.Uri = new(server.FileUri);
        feed.SaveXml(feedStream);
        var feedData = feedStream.ToArray();
        feedStream.Position = 0;

        AssertRefreshData(feed, feedData);
    }

    [Fact]
    public void RefreshClearsInMemoryCache()
    {
        var feed = FeedTest.CreateTestFeed();
        var feedStream = new MemoryStream();
        using var server = new MicroServer("feed.xml", feedStream);
        feed.Uri = new(server.FileUri);
        feed.SaveXml(feedStream);
        var feedData = feedStream.ToArray();
        feedStream.Position = 0;

        // Cause feed to become in-memory cached
        _feedCacheMock.Setup(x => x.GetFeed(feed.Uri)).Returns(feed);
        _feedManager[feed.Uri].Should().Be(feed);

        AssertRefreshData(feed, feedData);
    }

    private void AssertRefreshData(Feed feed, byte[] feedData)
    {
        _feedCacheMock.Setup(x => x.Add(feed.Uri, feedData));
        _feedCacheMock.Setup(x => x.GetFeed(feed.Uri)).Returns(feed);
        _feedCacheMock.Setup(x => x.GetSignatures(feed.Uri)).Returns(new[] {OpenPgpUtilsTest.TestSignature});

        // ReSharper disable once AccessToDisposedClosure
        _trustManagerMock.Setup(x => x.CheckTrust(feedData, feed.Uri, It.IsAny<OpenPgpKeyCallback>())).Returns(OpenPgpUtilsTest.TestSignature);

        _feedManager.Refresh = true;
        _feedManager[feed.Uri!].Should().Be(feed);
    }

    [Fact]
    public void DetectStaleCached()
    {
        var feed = new Feed {Name = "Mock feed"};
        _feedCacheMock.Setup(x => x.GetFeed(FeedTest.Test1Uri)).Returns(feed);
        new FeedPreferences {LastChecked = DateTime.UtcNow - _config.Freshness}.SaveFor(FeedTest.Test1Uri);

        _feedManager.IsStale(FeedTest.Test1Uri).Should().BeTrue();
        _feedManager[FeedTest.Test1Uri].Should().BeSameAs(feed);
        _feedManager.Stale.Should().BeTrue();
    }

    [Fact]
    public void RateLimit()
    {
        _feedManager.RateLimit(FeedTest.Test1Uri).Should().BeFalse();
        _feedManager.RateLimit(FeedTest.Test1Uri).Should().BeTrue();
    }

    [Fact] // Ensures valid feeds are correctly imported.
    public void Import()
    {
        var (uri, data) = FakeSignedFeed();

        // No previous feed
        _feedCacheMock.Setup(x => x.GetSignatures(uri)).Returns([]);

        // Adding new feed
        _feedCacheMock.Setup(x => x.Add(uri, data));

        _feedManager.ImportFeed(data.ToStream());
    }

    [Fact] // Ensures replay attacks are detected.
    public void ImportReplayAttack()
    {
        var (uri, data) = FakeSignedFeed();

        // Newer signature present => replay attack
        _feedCacheMock.Setup(x => x.GetSignatures(uri)).Returns(new[]
        {
            new ValidSignature(OpenPgpUtilsTest.TestKeyID, OpenPgpUtilsTest.TestFingerprint, new DateTime(2002, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        });

        Assert.Throws<ReplayAttackException>(() => _feedManager.ImportFeed(data.ToStream()));
    }

    private (FeedUri uri, byte[] data) FakeSignedFeed()
    {
        var feed = FeedTest.CreateTestFeed();
        var data = feed.ToXmlString().ToStream().ToArray();
        _trustManagerMock.Setup(x => x.CheckTrust(data, feed.Uri, It.IsAny<OpenPgpKeyCallback>())).Returns(OpenPgpUtilsTest.TestSignature);
        return (feed.Uri, data);
    }
}
