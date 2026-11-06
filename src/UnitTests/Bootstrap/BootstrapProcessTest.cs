// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Bootstrap;

public class BootstrapProcessTest
{
    [Theory]
    [InlineData("sha256new_ABC")]
    [InlineData("sha256new_ABC.tgz")]
    [InlineData("sha256new_ABC.tar.gz")]
    public void TryParseDigestIgnoresFileExtensions(string fileName)
        => (BootstrapProcess.TryParseDigest(fileName)?.Sha256New).Should().Be("ABC");

    [Theory]
    [InlineData("sha256new_ABC.tar.gz", true)]
    [InlineData("sha1new=abc.zip", true)]
    [InlineData("readme.txt", false)]
    [InlineData("88C8A1F375928691.gpg", false)]
    public void TryParseDigestDetectsDigests(string fileName, bool expected)
        => (BootstrapProcess.TryParseDigest(fileName) != null).Should().Be(expected);
}
