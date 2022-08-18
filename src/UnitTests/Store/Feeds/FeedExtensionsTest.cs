// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Feeds;

/// <summary>
/// Contains test methods for <see cref="FeedExtensions"/>.
/// </summary>
public class FeedExtensionsTest
{
    /// <summary>
    /// Ensures <see cref="FeedExtensions.FindImplementation"/> correctly locates <see cref="Implementation"/> in a list of <see cref="Feed"/>s.
    /// </summary>
    [Fact]
    public void FindImplementation()
    {
        var digest1 = new ManifestDigest(Sha256: "123");
        var implementation1 = new Implementation {ManifestDigest = digest1};
        var feed1 = new Feed {Elements = {implementation1}};
        var digest2 = new ManifestDigest(Sha256: "abc");
        var implementation2 = new Implementation {ManifestDigest = digest2};
        var feed2 = new Feed {Elements = {implementation2}};
        var feeds = new[] {feed1, feed2};

        feeds.FindImplementation(digest1).Should().Be((implementation1, feed1));
        feeds.FindImplementation(digest2).Should().Be((implementation2, feed2));
        feeds.FindImplementation(new ManifestDigest(Sha256: "invalid")).Should().BeNull(because: "No implementation should have been found");
    }
}
