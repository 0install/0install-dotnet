// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="ManifestDigest"/>.
/// </summary>
public class ManifestDigestTest
{
    /// <summary>
    /// Ensures <see cref="ManifestDigest.ParseID"/> correctly extracts additional digests from ID strings.
    /// </summary>
    [Fact]
    public void ParseID()
    {
        new ManifestDigest("sha1=test").Sha1.Should().Be("test");
        new ManifestDigest("sha1new=test").Sha1New.Should().Be("test");
        new ManifestDigest("sha256=test").Sha256.Should().Be("test");
        new ManifestDigest("sha256new_test").Sha256New.Should().Be("test");

        // Once a digest value has been set, ID values shall not be able to overwrite it
        var digest = new ManifestDigest("sha1=test");
        digest.ParseID("sha1=test2");
        digest.Sha1.Should().Be("test");
    }

    /// <summary>
    /// Ensures <see cref="ManifestDigest.PartialEquals"/> correctly compares digests.
    /// </summary>
    [Fact]
    public void PartialEqual()
    {
        var digest1 = new ManifestDigest(sha1: "test1");
        var digest2 = new ManifestDigest(sha1: "test1", sha1New: "test2");
        digest1.PartialEquals(digest2).Should().BeTrue();

        digest1 = new ManifestDigest(sha1: "test1");
        digest2 = new ManifestDigest(sha1: "test2");
        digest1.PartialEquals(digest2).Should().BeFalse();

        digest1 = new ManifestDigest(sha1: "test1");
        digest2 = new ManifestDigest(sha1New: "test2");
        digest1.PartialEquals(digest2).Should().BeFalse();

        digest1 = new ManifestDigest(sha1New: "test1");
        digest2 = new ManifestDigest(sha256: "test2");
        digest1.PartialEquals(digest2).Should().BeFalse();
    }
}
