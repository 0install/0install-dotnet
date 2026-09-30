// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Implementations;

public class StoreServicePathsTest
{
    [Theory]
    [InlineData("file")]
    [InlineData("dir/file.txt")]
    [InlineData(".hidden")]
    [InlineData("auxiliary.c/con-file")]
    [InlineData("with space/and~tilde")]
    [InlineData("unicode/äöü")]
    public void AcceptsValidPaths(string path)
        => StoreServicePaths.ToNativePath(path).Should().Be(path.Replace('/', Path.DirectorySeparatorChar));

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("a/..")]
    [InlineData("a/./b")]
    [InlineData("/a")]
    [InlineData("a/")]
    [InlineData("a//b")]
    [InlineData("a\\b")]
    [InlineData("a:b")]
    [InlineData("C:")]
    [InlineData("a*")]
    [InlineData("a?")]
    [InlineData("a<b")]
    [InlineData("a\tb")]
    [InlineData("a\u007Fb")]
    [InlineData("dot.")]
    [InlineData("space ")]
    [InlineData("NUL")]
    [InlineData("nul.txt")]
    [InlineData("dir/CON")]
    [InlineData("COM1.log")]
    [InlineData("LPT¹")]
    [InlineData("CONIN$")]
    public void RejectsInvalidPaths(string path)
        => ((Action)(() => StoreServicePaths.ToNativePath(path))).Should().Throw<InvalidDataException>();

    [Fact]
    public void RejectsOverlongPaths()
    {
        ((Action)(() => StoreServicePaths.ToNativePath(new string('a', 256)))).Should().Throw<InvalidDataException>();
        ((Action)(() => StoreServicePaths.ToNativePath(string.Join("/", Enumerable.Repeat("a", 65))))).Should().Throw<InvalidDataException>();
        ((Action)(() => StoreServicePaths.ToNativePath(string.Join("/", Enumerable.Repeat(new string('a', 200), 6))))).Should().Throw<InvalidDataException>();
    }

    private const string Sha256New = "sha256new_4OYMIQUY7QOBJGX36TEJS35ZEQT24QPEMSNZGTFESWMRW6CSXBKQ";
    private const string Sha256 = "sha256=e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private const string Sha1New = "sha1new=da39a3ee5e6b4b0d3255bfef95601890afd80709";

    [Fact]
    public void SelectsStrongestDigestOnly()
    {
        StoreServicePaths.ToStrongDigest([Sha1New, Sha256, Sha256New])
                         .Should().Be(new ManifestDigest(Sha256New));
        StoreServicePaths.ToStrongDigest([Sha1New, Sha256])
                         .Should().Be(new ManifestDigest(Sha256));
    }

    [Fact]
    public void RejectsWeakDigestsOnly()
        => ((Action)(() => StoreServicePaths.ToStrongDigest([Sha1New]))).Should().Throw<NotSupportedException>();

    [Theory]
    [InlineData("sha256new_../../x")]
    [InlineData("sha256new_4oymiquy7qobjgx36tejs35zeqt24qpemsnzgtfeswmrw6csxbkq")]
    [InlineData("sha256=../x")]
    [InlineData("sha256=e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b85")]
    [InlineData("sha256new_4OYMIQUY7QOBJGX36TEJS35ZEQT24QPEMSNZGTFESWMRW6CSXBKQ\n")]
    [InlineData("unknown=abc")]
    public void RejectsMalformedDigests(string digest)
        => ((Action)(() => StoreServicePaths.ToStrongDigest([digest]))).Should().Throw<InvalidDataException>();
}
