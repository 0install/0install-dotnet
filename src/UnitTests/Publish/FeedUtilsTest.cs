// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using FluentAssertions;
using Moq;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using Xunit;
using ZeroInstall.Model;
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
        var secretKey = new OpenPgpSecretKey(keyID: 123, fingerprint: new byte[] {1, 2, 3}, userID: "user");

        var openPgpMock = CreateMock<IOpenPgp>();
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
            expectedFeed = stream.ReadToString()
                         + Store.Feeds.FeedUtils.SignatureBlockStart
                         + Convert.ToBase64String(signature) + "\n"
                         + Store.Feeds.FeedUtils.SignatureBlockEnd;
        }

        signedFeed.Should().Be(expectedFeed, because: "Feed should remain unchanged except for appended XML signatre");
    }
}
