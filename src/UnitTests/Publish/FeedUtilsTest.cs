// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish;

/// <summary>
/// Contains test methods for <see cref="FeedUtils"/>.
/// </summary>
public class FeedUtilsTest : TestWithMocks
{
    /// <summary>
    /// Ensures <see cref="FeedUtils.SignFeed"/> produces valid feed files.
    /// </summary>
    [Fact]
    public void SignFeed()
    {
        var feed = new Feed {Name = "Test"};
        const string passphrase = "passphrase123";
        var signature = new byte[] {1, 2, 3};
        var secretKey = new OpenPgpSecretKey(KeyID: 123, new OpenPgpFingerprint([1, 2, 3]), UserID: "user");

        var openPgpMock = GetMock<IOpenPgp>();
        openPgpMock.Setup(x => x.Sign(It.IsAny<ArraySegment<byte>>(), secretKey, passphrase))
                   .Returns(signature);

        string signedFeed;
        using (var stream = new MemoryStream())
        {
            feed.SaveXml(stream);
            FeedUtils.SignFeed(stream, secretKey, passphrase, openPgpMock.Object);
            signedFeed = stream.ReadToString();
        }

        string expectedFeed;
        using (var stream = new MemoryStream())
        {
            feed.SaveXml(stream);
            expectedFeed = $"{stream.ReadToString()}{Store.Feeds.FeedUtils.SignatureBlockStart}{Convert.ToBase64String(signature)}\n{Store.Feeds.FeedUtils.SignatureBlockEnd}";
        }

        signedFeed.Should().Be(expectedFeed, because: "Feed should remain unchanged except for appended XML signatre");
    }
}
