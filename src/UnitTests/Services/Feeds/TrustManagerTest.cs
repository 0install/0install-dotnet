// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Moq;
using NanoByte.Common.Net;
using NanoByte.Common.Streams;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Store;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds
{
    /// <summary>
    /// Contains test methods for <see cref="TrustManager"/>.
    /// </summary>
    [Collection("Static state")]
    public class TrustManagerTest : TestWithMocks
    {
        #region Constants
        private const string FeedText = "Feed data\n";
        private readonly byte[] _feedBytes = Encoding.UTF8.GetBytes(FeedText);
        private static readonly byte[] _signatureBytes = Encoding.UTF8.GetBytes("Signature data");
        private static readonly string _signatureBase64 = Convert.ToBase64String(_signatureBytes).Insert(10, "\n");
        private static readonly byte[] _combinedBytes = Encoding.UTF8.GetBytes(FeedText + FeedUtils.SignatureBlockStart + _signatureBase64 + FeedUtils.SignatureBlockEnd);

        private static readonly byte[] _keyData = Encoding.ASCII.GetBytes("key");

        private const string KeyInfoResponse = @"<?xml version='1.0'?><key-lookup><item vote=""good"">Key information</item></key-lookup>";
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
            _feedCacheMock = CreateMock<IFeedCache>();
            _openPgpMock = CreateMock<IOpenPgp>();
            _trustManager = new TrustManager(_config, _openPgpMock.Object, _trustDB, _feedCacheMock.Object, _handler);
        }

        [Fact]
        public void PreviouslyTrusted()
        {
            RegisterKey();
            TrustKey();

            _trustManager.CheckTrust(_combinedBytes, new FeedUri("http://localhost/test.xml"))
                         .Should().Be(OpenPgpUtilsTest.TestSignature);
        }

        [Fact]
        public void BadSignature()
        {
            _openPgpMock.Setup(x => x.Verify(_feedBytes, _signatureBytes)).Returns(new OpenPgpSignature[] {new BadSignature(keyID: 123)});

            Assert.Throws<SignatureException>(() => _trustManager.CheckTrust(_combinedBytes, new FeedUri("http://localhost/test.xml")));
            IsKeyTrusted().Should().BeFalse(because: "Key should not be trusted");
        }

        [Fact]
        public void MultipleSignatures()
        {
            _openPgpMock.Setup(x => x.Verify(_feedBytes, _signatureBytes)).Returns(new OpenPgpSignature[] {new BadSignature(keyID: 123), OpenPgpUtilsTest.TestSignature});
            TrustKey();

            _trustManager.CheckTrust(_combinedBytes, new FeedUri("http://localhost/test.xml"))
                         .Should().Be(OpenPgpUtilsTest.TestSignature);
        }

        [Fact]
        public void ExistingKeyAndReject()
        {
            RegisterKey();
            _handler.AnswerQuestionWith = false;

            Assert.Throws<SignatureException>(() => _trustManager.CheckTrust(_combinedBytes, new FeedUri("http://localhost/test.xml")));
            IsKeyTrusted().Should().BeFalse(because: "Key should not be trusted");
        }

        [Fact]
        public void ExistingKeyAndApprove()
        {
            RegisterKey();
            _handler.AnswerQuestionWith = true;

            _trustManager.CheckTrust(_combinedBytes, new FeedUri("http://localhost/test.xml"))
                         .Should().Be(OpenPgpUtilsTest.TestSignature);
            IsKeyTrusted().Should().BeTrue(because: "Key should be trusted");
        }

        [Fact]
        public void ExistingKeyAndNoAutoTrust()
        {
            RegisterKey();
            _feedCacheMock.Setup(x => x.Contains(new("http://localhost/test.xml"))).Returns(true);
            _handler.AnswerQuestionWith = false;

            using (var keyInfoServer = new MicroServer("key/" + OpenPgpUtilsTest.TestSignature.FormatFingerprint(), KeyInfoResponse.ToStream()))
            {
                UseKeyInfoServer(keyInfoServer);
                Assert.Throws<SignatureException>(() => _trustManager.CheckTrust(_combinedBytes, new FeedUri("http://localhost/test.xml")));
            }
            IsKeyTrusted().Should().BeFalse(because: "Key should not be trusted");
        }

        [Fact]
        public void ExistingKeyAndAutoTrust()
        {
            RegisterKey();
            _feedCacheMock.Setup(x => x.Contains(new("http://localhost/test.xml"))).Returns(false);

            using (var keyInfoServer = new MicroServer("key/" + OpenPgpUtilsTest.TestSignature.FormatFingerprint(), KeyInfoResponse.ToStream()))
            {
                UseKeyInfoServer(keyInfoServer);
                _trustManager.CheckTrust(_combinedBytes, new FeedUri("http://localhost/test.xml"))
                             .Should().Be(OpenPgpUtilsTest.TestSignature);
            }
            IsKeyTrusted().Should().BeTrue(because: "Key should be trusted");
        }

        [Fact]
        public void DownloadKeyAndReject()
        {
            ExpectKeyImport();
            _handler.AnswerQuestionWith = false;

            using (var server = new MicroServer(OpenPgpUtilsTest.TestKeyIDString + ".gpg", new MemoryStream(_keyData)))
                Assert.Throws<SignatureException>(() => _trustManager.CheckTrust(_combinedBytes, new FeedUri(server.ServerUri + "test.xml")));
            IsKeyTrusted().Should().BeFalse(because: "Key should not be trusted");
        }

        [Fact]
        public void DownloadKeyAndApprove()
        {
            ExpectKeyImport();
            _handler.AnswerQuestionWith = true;

            using (var server = new MicroServer(OpenPgpUtilsTest.TestKeyIDString + ".gpg", new MemoryStream(_keyData)))
            {
                _trustManager.CheckTrust(_combinedBytes, new FeedUri(server.ServerUri + "test.xml"))
                             .Should().Be(OpenPgpUtilsTest.TestSignature);
            }
            IsKeyTrusted().Should().BeTrue(because: "Key should be trusted");
        }

        [Fact]
        public void DownloadKeyFromMirrorAndApprove()
        {
            ExpectKeyImport();
            _handler.AnswerQuestionWith = true;

            using (var server = new MicroServer("keys/" + OpenPgpUtilsTest.TestKeyIDString + ".gpg", new MemoryStream(_keyData)))
            {
                _config.FeedMirror = new FeedUri(server.ServerUri);
                _trustManager.CheckTrust(_combinedBytes, new FeedUri("http://localhost:9999/test/feed.xml"))
                             .Should().Be(OpenPgpUtilsTest.TestSignature);
            }
            IsKeyTrusted().Should().BeTrue(because: "Key should be trusted");
        }

        private void RegisterKey()
            => _openPgpMock.Setup(x => x.Verify(_feedBytes, _signatureBytes)).Returns(new OpenPgpSignature[] {OpenPgpUtilsTest.TestSignature});

        private void TrustKey()
            => _trustDB.TrustKey(OpenPgpUtilsTest.TestSignature.FormatFingerprint(), new Domain("localhost"));

        private bool IsKeyTrusted()
            => _trustDB.IsTrusted(OpenPgpUtilsTest.TestSignature.FormatFingerprint(), new Domain {Value = "localhost"});

        private void ExpectKeyImport()
        {
            _openPgpMock.SetupSequence(x => x.Verify(_feedBytes, _signatureBytes))
                        .Returns(new OpenPgpSignature[] {new MissingKeySignature(OpenPgpUtilsTest.TestKeyID)})
                        .Returns(new OpenPgpSignature[] {OpenPgpUtilsTest.TestSignature});
            _openPgpMock.Setup(x => x.ImportKey(_keyData));
        }

        private void UseKeyInfoServer(MicroServer keyInfoServer)
        {
            _config.AutoApproveKeys = true;
            _config.KeyInfoServer = new FeedUri(keyInfoServer.ServerUri);
        }
    }
}
