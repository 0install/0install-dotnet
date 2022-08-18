// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="ManifestDigest"/>.
/// </summary>
public class ManifestDigestTest
{
    [Fact]
    public void ConstructorSingleAlgorithm()
    {
        new ManifestDigest("sha1=test").Sha1.Should().Be("test");
        new ManifestDigest("sha1=test").Sha1.Should().Be("test");
        new ManifestDigest("sha1new=test").Sha1New.Should().Be("test");
        new ManifestDigest("sha256=test").Sha256.Should().Be("test");
        new ManifestDigest("sha256new_test").Sha256New.Should().Be("test");
    }

    [Fact]
    public void ConstructorMultipleAlgorithms()
    {
        new ManifestDigest("sha1new=test1,sha256new_test2,invalid").AvailableDigests.Should().Equal("sha256new_test2", "sha1new=test1");
    }

    [Fact]
    public void ConstructorInvalid()
    {
        new Action(() => _ = new ManifestDigest("invalid")).Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void TryParseSuccess()
    {
        var digest = new ManifestDigest();
        digest.TryParse("sha1new=test1");
        digest.TryParse("sha256new_test2");
        digest.AvailableDigests.Should().Equal("sha256new_test2", "sha1new=test1");
    }

    [Fact]
    public void TryParseEmptyString()
    {
        var digest = new ManifestDigest();
        digest.TryParse("");
        digest.AvailableDigests.Should().BeEmpty();
    }

    [Fact]
    public void TryParseInvalid()
    {
        var digest = new ManifestDigest();
        digest.TryParse("invalid");
        digest.AvailableDigests.Should().BeEmpty();
    }

    [Fact]
    public void ParseIDNoOverwrite()
    {
        var digest = new ManifestDigest("sha1=test");
        digest.TryParse("sha1=test2");
        digest.Sha1.Should().Be("test", because: "Once a digest value has been set, ID values should not overwrite it");
    }

    [Fact]
    public void PartialEqual()
    {
        var digest1 = new ManifestDigest(Sha1: "test1");
        var digest2 = new ManifestDigest(Sha1: "test1", Sha1New: "test2");
        digest1.PartialEquals(digest2).Should().BeTrue();

        digest1 = new ManifestDigest(Sha1: "test1");
        digest2 = new ManifestDigest(Sha1: "test2");
        digest1.PartialEquals(digest2).Should().BeFalse();

        digest1 = new ManifestDigest(Sha1: "test1");
        digest2 = new ManifestDigest(Sha1New: "test2");
        digest1.PartialEquals(digest2).Should().BeFalse();

        digest1 = new ManifestDigest(Sha1New: "test1");
        digest2 = new ManifestDigest(Sha256: "test2");
        digest1.PartialEquals(digest2).Should().BeFalse();
    }
}
