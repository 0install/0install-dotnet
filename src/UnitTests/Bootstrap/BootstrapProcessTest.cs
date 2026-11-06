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

    [Theory]
    [InlineData(new[] {"--batch", "run", "--offline", "vlc", "--refresh", "-v"}, new[] {"--batch", "--offline"})]
    [InlineData(new[] {"run", "vlc", "--offline"}, new string[0])]
    [InlineData(new[] {"--offline", "--", "--batch"}, new[] {"--offline"})]
    [InlineData(new string[0], new string[0])]
    public void GetLeadingOptionsIgnoresAppArguments(string[] args, string[] expected)
        => BootstrapProcess.GetLeadingOptions(args).Should().Equal(expected);

    [Theory]
    [InlineData(new[] {"--batch", "run", "central"}, "run")]
    [InlineData(new[] {"central"}, "central")]
    [InlineData(new[] {"--help"}, null)]
    public void GetCommandReturnsFirstNonOption(string[] args, string? expected)
        => BootstrapProcess.GetCommand(args).Should().Be(expected);
}
