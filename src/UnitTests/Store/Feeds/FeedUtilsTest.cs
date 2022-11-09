// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Store.Feeds;

/// <summary>
/// Contains test methods for <see cref="FeedUtils"/>.
/// </summary>
public class FeedUtilsTest : TestWithMocks
{
    private const string FeedText = "Feed data\n";
    private readonly ArraySegment<byte> _feedBytes = new(Encoding.UTF8.GetBytes(FeedText));
    private static readonly byte[] _signatureBytes = Encoding.UTF8.GetBytes("Signature data");
    private static readonly string _signatureBase64 = Convert.ToBase64String(_signatureBytes).Insert(10, "\n");

    /// <summary>
    /// Ensures that <see cref="FeedUtils.GetSignatures"/> correctly separates an XML signature block from a signed feed.
    /// </summary>
    [Fact]
    public void GetSignatures()
    {
        var openPgpMock = GetMock<IOpenPgp>();
        var result = new OpenPgpSignature[] {OpenPgpUtilsTest.TestSignature};
        openPgpMock.Setup(x => x.Verify(_feedBytes, _signatureBytes)).Returns(result);

        string input = FeedText + FeedUtils.SignatureBlockStart + _signatureBase64 + FeedUtils.SignatureBlockEnd;
        FeedUtils.GetSignatures(openPgpMock.Object, Encoding.UTF8.GetBytes(input)).Should().Equal(result);
    }

    /// <summary>
    /// Ensures that <see cref="FeedUtils.GetSignatures"/> throws a <see cref="SignatureException"/> if the signature block does not start in a new line.
    /// </summary>
    [Fact]
    public void GetSignaturesMissingNewLine()
    {
        string input = "Feed without newline" + FeedUtils.SignatureBlockStart + _signatureBase64 + FeedUtils.SignatureBlockEnd;
        Assert.Throws<SignatureException>(() => FeedUtils.GetSignatures(Mock.Of<IOpenPgp>(), Encoding.UTF8.GetBytes(input)));
    }

    /// <summary>
    /// Ensures that <see cref="FeedUtils.GetSignatures" /> throws a <see cref="SignatureException"/> if the signature contains non-base 64 characters.
    /// </summary>
    [Fact]
    public void GetSignaturesInvalidChars()
    {
        const string input = FeedText + FeedUtils.SignatureBlockStart + "*!?#" + FeedUtils.SignatureBlockEnd;
        Assert.Throws<SignatureException>(() => FeedUtils.GetSignatures(Mock.Of<IOpenPgp>(), Encoding.UTF8.GetBytes(input)));
    }

    /// <summary>
    /// Ensures that <see cref="FeedUtils.GetSignatures" /> throws a <see cref="SignatureException"/> if the correct signature end is missing.
    /// </summary>
    [Fact]
    public void GetSignaturesMissingEnd()
    {
        string input = FeedText + FeedUtils.SignatureBlockStart + _signatureBase64;
        Assert.Throws<SignatureException>(() => FeedUtils.GetSignatures(Mock.Of<IOpenPgp>(), Encoding.UTF8.GetBytes(input)));
    }

    /// <summary>
    /// Ensures that <see cref="FeedUtils.GetSignatures" /> throws a <see cref="SignatureException"/> if there is additional data after the signature block.
    /// </summary>
    [Fact]
    public void GetSignaturesDataAfterSignature()
    {
        string input = FeedText + FeedUtils.SignatureBlockStart + _signatureBase64 + FeedUtils.SignatureBlockEnd + "more data";
        Assert.Throws<SignatureException>(() => FeedUtils.GetSignatures(Mock.Of<IOpenPgp>(), Encoding.UTF8.GetBytes(input)));
    }
}
