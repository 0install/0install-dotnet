// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NanoByte.Common;
using NanoByte.Common.Streams;
using Xunit;

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// Contains common code for testing specific <see cref="IOpenPgp"/> implementations.
    /// </summary>
    public abstract class OpenPgpTest
    {
        protected abstract IOpenPgp OpenPgp { get; }

        private readonly OpenPgpSecretKey _secretKey = new(
            keyID: OpenPgpUtils.ParseKeyID("DEED44B49BE24661"),
            fingerprint: OpenPgpUtils.ParseFingerprint("E91FE1CBFCCF315543F6CB13DEED44B49BE24661"),
            userID: "Test User <test@0install.de>");

        private readonly ArraySegment<byte> _referenceData = new(new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9});

        private readonly byte[] _signatureData = typeof(OpenPgpTest).GetEmbeddedBytes("signature.dat");

        private ArraySegment<byte> DummyData => new(new byte[] {1, 2, 3});

        [Fact]
        public void VerifyValidSignature()
        {
            ImportKey();
            OpenPgp.Verify(_referenceData, _signatureData).Should().Equal(
                new ValidSignature(_secretKey.KeyID, _secretKey.GetFingerprint(), new DateTime(2015, 7, 16, 17, 20, 7, DateTimeKind.Utc)));
        }

        [Fact]
        public void VerifyBadSignature()
        {
            ImportKey();
            OpenPgp.Verify(DummyData, _signatureData).Should().Equal(new BadSignature(_secretKey.KeyID));
        }

        [Fact]
        public void VerifyMissingKeySignature()
            => OpenPgp.Verify(_referenceData, _signatureData).Should().Equal(new MissingKeySignature(_secretKey.KeyID));

        [Fact]
        public void VerifyInvalidData()
            => Assert.Throws<InvalidDataException>(() => OpenPgp.Verify(DummyData, new byte[] {1, 2, 3}));

        [Fact]
        public void Sign()
        {
            DeployKeyRings();

            var signatureData = OpenPgp.Sign(_referenceData, _secretKey, "passphrase");
            signatureData.Length.Should().BeGreaterThan(10);

            ImportKey();
            var signature = (ValidSignature)OpenPgp.Verify(_referenceData, signatureData).Single();
            signature.GetFingerprint().Should().Equal(_secretKey.GetFingerprint());
        }

        [Fact]
        public void SignMissingKey()
            => Assert.Throws<KeyNotFoundException>(() => OpenPgp.Sign(_referenceData, _secretKey));

        [Fact]
        public void SignWrongPassphrase()
        {
            DeployKeyRings();
            Assert.Throws<WrongPassphraseException>(() => OpenPgp.Sign(_referenceData, _secretKey, "wrong-passphrase"));
        }

        [Fact]
        public void ImportKey()
            => OpenPgp.ImportKey(typeof(OpenPgpTest).GetEmbeddedStream("pubkey.gpg"));

        [Fact]
        public void ImportKeyInvalidData()
            => Assert.Throws<InvalidDataException>(() => OpenPgp.ImportKey(new MemoryStream(new byte[] {1, 2, 3})));

        [Fact]
        public void ExportKey()
        {
            DeployKeyRings();

            string exportedKey = OpenPgp.ExportKey(_secretKey);
            string referenceKeyData = typeof(OpenPgpTest).GetEmbeddedString("pubkey.gpg").GetRightPartAtFirstOccurrence("\n\n").GetLeftPartAtLastOccurrence("+");

            exportedKey.Should().StartWith("-----BEGIN PGP PUBLIC KEY BLOCK-----\n");
            exportedKey.Should().Contain(referenceKeyData);
            exportedKey.Should().EndWith("-----END PGP PUBLIC KEY BLOCK-----\n");
        }

        [Fact]
        public void ExportKeyMissingKey()
            => Assert.Throws<KeyNotFoundException>(() => OpenPgp.ExportKey(_secretKey));

        [Fact]
        public void ListSecretKeys()
        {
            DeployKeyRings();
            OpenPgp.ListSecretKeys().Should().Equal(_secretKey);
        }

        [Fact]
        public void GetSecretKey()
        {
            DeployKeyRings();

            OpenPgp.GetSecretKey(_secretKey).Should().Be(_secretKey, because: "Should get secret key using parsed id source");

            OpenPgp.GetSecretKey(_secretKey.UserID).Should().Be(_secretKey, because: "Should get secret key using user id");
            OpenPgp.GetSecretKey(_secretKey.FormatKeyID()).Should().Be(_secretKey, because: "Should get secret key using key id string");
            OpenPgp.GetSecretKey(_secretKey.FormatFingerprint()).Should().Be(_secretKey, because: "Should get secret key using fingerprint string");

            OpenPgp.GetSecretKey().Should().Be(_secretKey, because: "Should get default secret key");

            Assert.Throws<KeyNotFoundException>(() => OpenPgp.GetSecretKey("unknown@user.com"));
        }

        protected abstract void DeployKeyRings();
    }
}
