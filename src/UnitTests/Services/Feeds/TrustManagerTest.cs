// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using NanoByte.Common.Net;
using NanoByte.Common.Streams;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds;

/// <summary>
/// Contains test methods for <see cref="TrustManager"/>.
/// </summary>
public class TrustManagerTest : TestWithMocks
{
    #region Constants
    private const string FeedText = "Feed data\n";
    private readonly ArraySegment<byte> _feedBytes = new(Encoding.UTF8.GetBytes(FeedText));
    private static readonly byte[] _signatureBytes = Encoding.UTF8.GetBytes("Signature data");
    private static readonly string _signatureBase64 = Convert.ToBase64String(_signatureBytes).Insert(10, "\n");
    private static readonly byte[] _combinedBytes = Encoding.UTF8.GetBytes(FeedText + FeedUtils.SignatureBlockStart + _signatureBase64 + FeedUtils.SignatureBlockEnd);

    private const string KeyInfoResponse = @"<?xml version='1.0'?><key-lookup><item vote=""good"">Key information</item></key-lookup>";

    private static readonly byte[] _keyBytes = EncodingUtils.Utf8.GetBytes("key");
    private static Stream KeyStream => _keyBytes.ToStream();
    #endregion

    private readonly Config _config = new()
    {
        KeyInfoServer = null,
        AutoApproveKeys = false
    };

    private readonly TrustDB _trustDB = new();
    private readonly MockTaskHandler _handler = new();
    private readonly Mock<IOpenPgp> _openPgpMock;
    private readonly Mock<IFeedCache> _feedCacheMock;
    private readonly ITrustManager _trustManager;

    public TrustManagerTest()
    {
        _feedCacheMock = GetMock<IFeedCache>();
        _openPgpMock = GetMock<IOpenPgp>();
        _trustManager = new TrustManager(_trustDB, _config, _openPgpMock.Object, _feedCacheMock.Object, _handler);
    }

    [Fact]
    public void PreviouslyTrusted()
    {
        RegisterKey();
        TrustKey();

        _trustManager.CheckTrust(_combinedBytes, new("http://localhost/test.xml"))
                     .Should().Be(OpenPgpUtilsTest.TestSignature);
    }

    [Fact]
    public void BadSignature()
    {
        _openPgpMock.Setup(x => x.Verify(_feedBytes, _signatureBytes)).Returns(new OpenPgpSignature[] {new BadSignature(KeyID: 123)});

        Assert.Throws<SignatureException>(() => _trustManager.CheckTrust(_combinedBytes, new("http://localhost/test.xml")));
        IsKeyTrusted().Should().BeFalse(because: "Key should not be trusted");
    }

    [Fact]
    public void MultipleSignatures()
    {
        _openPgpMock.Setup(x => x.Verify(_feedBytes, _signatureBytes)).Returns(new OpenPgpSignature[] {new BadSignature(KeyID: 123), OpenPgpUtilsTest.TestSignature});
        TrustKey();

        _trustManager.CheckTrust(_combinedBytes, new("http://localhost/test.xml"))
                     .Should().Be(OpenPgpUtilsTest.TestSignature);
    }

    [Fact]
    public void ExistingKeyAndReject()
    {
        RegisterKey();
        _handler.AnswerQuestionWith = false;

        Assert.Throws<SignatureException>(() => _trustManager.CheckTrust(_combinedBytes, new("http://localhost/test.xml")));
        IsKeyTrusted().Should().BeFalse(because: "Key should not be trusted");
    }

    [Fact]
    public void ExistingKeyAndApprove()
    {
        RegisterKey();
        _handler.AnswerQuestionWith = true;

        _trustManager.CheckTrust(_combinedBytes, new("http://localhost/test.xml"))
                     .Should().Be(OpenPgpUtilsTest.TestSignature);
        IsKeyTrusted().Should().BeTrue(because: "Key should be trusted");
    }

    [Fact]
    public void ExistingKeyAndNoAutoTrust()
    {
        RegisterKey();
        _feedCacheMock.Setup(x => x.Contains(new("http://localhost/test.xml"))).Returns(true);
        _handler.AnswerQuestionWith = false;

        using (var keyInfoServer = new MicroServer($"key/{OpenPgpUtilsTest.TestSignature.FormatFingerprint()}", KeyInfoResponse.ToStream()))
        {
            UseKeyInfoServer(keyInfoServer);
            Assert.Throws<SignatureException>(() => _trustManager.CheckTrust(_combinedBytes, new("http://localhost/test.xml")));
        }
        IsKeyTrusted().Should().BeFalse(because: "Key should not be trusted");
    }

    [Fact]
    public void ExistingKeyAndAutoTrust()
    {
        RegisterKey();
        _feedCacheMock.Setup(x => x.Contains(new("http://localhost/test.xml"))).Returns(false);

        using (var keyInfoServer = new MicroServer($"key/{OpenPgpUtilsTest.TestSignature.FormatFingerprint()}", KeyInfoResponse.ToStream()))
        {
            UseKeyInfoServer(keyInfoServer);
            _trustManager.CheckTrust(_combinedBytes, new("http://localhost/test.xml"))
                         .Should().Be(OpenPgpUtilsTest.TestSignature);
        }
        IsKeyTrusted().Should().BeTrue(because: "Key should be trusted");
    }

    [Fact]
    public void DownloadKeyAndReject()
    {
        ExpectKeyImport();
        _handler.AnswerQuestionWith = false;

        using (var server = new MicroServer($"{OpenPgpUtilsTest.TestKeyIDString}.gpg", KeyStream))
            Assert.Throws<SignatureException>(() => _trustManager.CheckTrust(_combinedBytes, new FeedUri($"{server.ServerUri}test.xml")));
        IsKeyTrusted().Should().BeFalse(because: "Key should not be trusted");
    }

    [Fact]
    public void DownloadKeyAndApprove()
    {
        ExpectKeyImport();
        _handler.AnswerQuestionWith = true;

        using (var server = new MicroServer($"{OpenPgpUtilsTest.TestKeyIDString}.gpg", KeyStream))
        {
            _trustManager.CheckTrust(_combinedBytes, new FeedUri($"{server.ServerUri}test.xml"))
                         .Should().Be(OpenPgpUtilsTest.TestSignature);
        }
        IsKeyTrusted().Should().BeTrue(because: "Key should be trusted");
    }

    [Fact]
    public void DownloadKeyFromMirrorAndApprove()
    {
        ExpectKeyImport();
        _handler.AnswerQuestionWith = true;

        const string exampleDomain = "example"; // Can't use "localhost" because mirror is not used for loopback URIs
        using (var server = new MicroServer($"keys/{OpenPgpUtilsTest.TestKeyIDString}.gpg", KeyStream))
        {
            _config.FeedMirror = new(server.ServerUri);
            _trustManager.CheckTrust(_combinedBytes, new($"http://{exampleDomain}:9999/test/feed.xml"))
                         .Should().Be(OpenPgpUtilsTest.TestSignature);
        }
        IsKeyTrusted(exampleDomain).Should().BeTrue(because: "Key should be trusted");
    }

    [Fact]
    public void LoadKeyFromCallbackAndApprove()
    {
        ExpectKeyImport();
        _handler.AnswerQuestionWith = true;

        using (var tempDir = new TemporaryDirectory("0install-test-trust"))
        {
            var feedUri = new FeedUri("http://localhost/test.xml");
            string keyPath = Path.Combine(tempDir, $"{OpenPgpUtilsTest.TestKeyIDString}.gpg");
            KeyStream.CopyToFile(keyPath);

            _trustManager.CheckTrust(_combinedBytes, feedUri,
                              keyCallback: id => id == OpenPgpUtilsTest.TestKeyIDString ? new(_keyBytes) : null)
                         .Should().Be(OpenPgpUtilsTest.TestSignature);
        }
        IsKeyTrusted().Should().BeTrue(because: "Key should be trusted");
    }

    private void RegisterKey()
        => _openPgpMock.Setup(x => x.Verify(_feedBytes, _signatureBytes)).Returns(new OpenPgpSignature[] {OpenPgpUtilsTest.TestSignature});

    private void TrustKey()
        => _trustDB.TrustKey(OpenPgpUtilsTest.TestSignature.FormatFingerprint(), new Domain("localhost"));

    private bool IsKeyTrusted(string domain = "localhost")
        => _trustDB.IsTrusted(OpenPgpUtilsTest.TestSignature.FormatFingerprint(), new Domain {Value = domain});

    private void ExpectKeyImport()
    {
        _openPgpMock.SetupSequence(x => x.Verify(_feedBytes, _signatureBytes))
                    .Returns(new OpenPgpSignature[] {new MissingKeySignature(OpenPgpUtilsTest.TestKeyID)})
                    .Returns(new OpenPgpSignature[] {OpenPgpUtilsTest.TestSignature});
        _openPgpMock.Setup(x => x.ImportKey(It.Is<ArraySegment<byte>>(segment => segment.SequenceEqual(_keyBytes))));
    }

    private void UseKeyInfoServer(MicroServer keyInfoServer)
    {
        _config.AutoApproveKeys = true;
        _config.KeyInfoServer = new FeedUri(keyInfoServer.ServerUri);
    }
}
