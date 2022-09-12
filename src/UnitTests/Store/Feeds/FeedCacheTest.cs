// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using NanoByte.Common.Native;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Store.Feeds;

/// <summary>
/// Contains test methods for <see cref="FeedCache"/>.
/// </summary>
public class FeedCacheTest : TestWithMocks
{
    private readonly TemporaryDirectory _tempDir;
    private readonly FeedCache _cache;
    private readonly Feed _feed1;

    public FeedCacheTest()
    {
        // Create a temporary cache
        _tempDir = new TemporaryDirectory("0install-test-feeds");
        _cache = new FeedCache(_tempDir, new Mock<IOpenPgp>().Object);

        // Add some dummy feeds to the cache
        _feed1 = FeedTest.CreateTestFeed();
        _feed1.Uri = FeedTest.Test1Uri;
        _feed1.SaveXml(Path.Combine(_tempDir, _feed1.Uri.Escape()));

        var feed2 = FeedTest.CreateTestFeed();
        feed2.Uri = FeedTest.Test2Uri;
        feed2.SaveXml(Path.Combine(_tempDir, feed2.Uri.Escape()));
        File.WriteAllText(Path.Combine(_tempDir, "http_invalid"), "");
    }

    public override void Dispose()
    {
        base.Dispose();
        _tempDir.Dispose();
    }

    [Fact]
    public void ContainsPositive()
    {
        _cache.Contains(FeedTest.Test1Uri).Should().BeTrue();
        _cache.Contains(FeedTest.Test2Uri).Should().BeTrue();
        _cache.Contains(FeedTest.Test3Uri).Should().BeFalse();
    }

    [Fact]
    public void ContainsNegative()
    {
        using var tempDir = new TemporaryDirectory("0install-test-feeds");
        _cache.Contains(new(Path.Combine(tempDir, "feed.xml"))).Should().BeFalse();
    }

    [Fact]
    public void ContainsLocalFilesOutsideOfActualCache()
    {
        using var localFeed = new TemporaryFile("0install-test-feed");
        _feed1.SaveXml(localFeed);
        _cache.Contains(new(localFeed)).Should().BeTrue();
    }

    [Fact]
    public void ContainsCaseSensitive()
    {
        _cache.Contains(FeedTest.Test1Uri)
              .Should().BeTrue();
        _cache.Contains(new(FeedTest.Test1Uri.OriginalString.ToUpperInvariant()))
              .Should().BeFalse(because: "Should be case-sensitive on all platforms");
    }

    [Fact]
    public void ListAll()
        => _cache.ListAll()
                 .Should().BeEquivalentTo(new[] {FeedTest.Test1Uri!, FeedTest.Test2Uri});

    [Fact]
    public void GetFeed()
        => _cache.GetFeed(_feed1.Uri!)
                 .Should().Be(_feed1);

    [Fact]
    public void GetFeedCaseSensitive()
    {
        _cache.GetFeed(FeedTest.Test1Uri)
              .Should().NotBeNull();
        _cache.GetFeed(new(FeedTest.Test1Uri.OriginalString.ToUpperInvariant()))
              .Should().BeNull(because: "Should be case-sensitive on all platforms");
    }

    [Fact]
    public void GetFeedMissing()
        => _cache.GetFeed(FeedTest.Test3Uri)
                 .Should().BeNull();

    [Fact]
    public void GetFeedCorrupt()
    {
        File.WriteAllText(Path.Combine(_tempDir, FeedTest.Test3Uri.Escape()), "corrupt");
        _cache.GetFeed(FeedTest.Test3Uri)
              .Should().BeNull();
    }

    [Fact]
    public void GetSignatures()
        => _cache.GetSignatures(FeedTest.Test1Uri)
                 .Should().BeEmpty();

    [Fact]
    public void GetSignaturesCorrupt()
    {
        File.WriteAllText(Path.Combine(_tempDir, FeedTest.Test3Uri.Escape()), "corrupt");
        _cache.GetSignatures(FeedTest.Test3Uri)
              .Should().BeEmpty();
    }

    [Fact]
    public void Add()
    {
        var feed = FeedTest.CreateTestFeed();
        feed.Uri = FeedTest.Test3Uri;

        _cache.Add(feed.Uri, ToArray(feed));

        _cache.GetFeed(feed.Uri)
              .Should().Be(feed);
    }

    [Fact]
    public void Remove()
    {
        _cache.Contains(FeedTest.Test1Uri).Should().BeTrue();
        _cache.Remove(FeedTest.Test1Uri);
        _cache.Contains(FeedTest.Test1Uri).Should().BeFalse();
        _cache.Contains(FeedTest.Test2Uri).Should().BeTrue();
    }

    /// <summary>
    /// Ensures <see cref="FeedCache"/> can handle feed URIs longer than the OSes maximum supported file path length.
    /// </summary>
    [SkippableFact(Skip = "Slow")]
    public void TestTooLongFilename()
    {
        Skip.IfNot(WindowsUtils.IsWindows, "Windows systems have a specific upper limit to file path lengths");

        var longHttpUrlBuilder = new StringBuilder(255);
        for (int i = 0; i < 255; i++)
            longHttpUrlBuilder.Append("x");

        var feed = FeedTest.CreateTestFeed();
        feed.Uri = new("http://example.com/-" + longHttpUrlBuilder);

        _cache.Add(feed.Uri, ToArray(feed));

        feed.Normalize(feed.Uri);
        _cache.GetFeed(feed.Uri!).Should().Be(feed);

        _cache.Contains(feed.Uri).Should().BeTrue();
        _cache.Remove(feed.Uri);
        _cache.Contains(feed.Uri).Should().BeFalse();
    }

    private static byte[] ToArray(Feed feed)
    {
        using var stream = new MemoryStream();
        feed.SaveXml(stream);
        return stream.ToArray();
    }
}
