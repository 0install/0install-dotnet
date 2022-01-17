// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Contains test methods for <see cref="OpenPgpUtils"/>.
/// </summary>
public class OpenPgpUtilsTest : TestWithMocks
{
    public const string TestKeyIDString = "00000000000000FF";
    public static readonly long TestKeyID = 255;
    public static readonly byte[] TestFingerprint = {170, 170, 0, 0, 0, 0, 0, 0, 0, 255};
    public const string TestFingerprintString = "AAAA00000000000000FF";
    public static readonly ValidSignature TestSignature = new(TestKeyID, TestFingerprint, new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void FormatKeyID()
        => new ErrorSignature(TestKeyID)
          .FormatKeyID()
          .Should().Be(TestKeyIDString);

    [Fact]
    public void FormatFingerprint()
        => new OpenPgpSecretKey(TestKeyID, TestFingerprint, "a@b.com")
          .FormatFingerprint()
          .Should().Be(TestFingerprintString);

    [Fact]
    public void ParseKeyID()
        => OpenPgpUtils.ParseKeyID(TestKeyIDString)
                       .Should().Be(TestKeyID);

    [Fact]
    public void ParseFingerpint()
        => OpenPgpUtils.ParseFingerprint(TestFingerprintString)
                       .Should().Equal(TestFingerprint);

    [Fact]
    public void FingerprintToKeyID()
        => OpenPgpUtils.FingerprintToKeyID(OpenPgpUtils.ParseFingerprint("E91FE1CBFCCF315543F6CB13DEED44B49BE24661"))
                       .Should().Be(OpenPgpUtils.ParseKeyID("DEED44B49BE24661"));

    [Fact]
    public void DeployPublicKey()
    {
        using var tempDir = new TemporaryDirectory("0install-test-openpgp");
        const string publicKey = "public";
        var secretKey = new OpenPgpSecretKey(keyID: 123, fingerprint: new byte[] {1, 2, 3}, userID: "user");

        var openPgpMock = CreateMock<IOpenPgp>();
        openPgpMock.Setup(x => x.ExportKey(secretKey)).Returns(publicKey);
        openPgpMock.Object.DeployPublicKey(secretKey, tempDir.Path);

        File.ReadAllText(tempDir + Path.DirectorySeparatorChar + secretKey.FormatKeyID() + ".gpg")
            .Should().Be(publicKey, because: "Public key should be written to parallel file in directory");
    }
}
