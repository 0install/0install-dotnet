// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="FeedUri"/>.
/// </summary>
[SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
public class FeedUriTest
{
    /// <summary>
    /// Ensures the <see cref="FeedUri"/> constructor correctly identify invalid interface URIs.
    /// </summary>
    [Fact]
    public void Valid()
    {
        new FeedUri("http://example.com");
        new FeedUri("http://example.com/");
        new FeedUri("http://example.com/test1.xml");
        new FeedUri("https://example.com/test1.xml");

        new FeedUri("http://example.com/my feed.xml");
        new FeedUri("http://example.com/my%20feed.xml");

        new FeedUri(WindowsUtils.IsWindows ? @"C:\my feed.xml" : "/root/my feed.xml");
        new FeedUri(WindowsUtils.IsWindows ? "file:///C:/my%20feed.xml" : "file:///root/my%20feed.xml");
        if (WindowsUtils.IsWindows)
        {
            new FeedUri(@"\\SERVER\C$\my feed.xml");
            new FeedUri("file://SERVER/C$/my%20feed.xml");
        }
    }

    /// <summary>
    /// Ensures the <see cref="FeedUri"/> constructor correctly identify valid interface URIs.
    /// </summary>
    [Theory, InlineData("ftp://host/"), InlineData("foo://host/"), InlineData("relative")]
    public void TestInvalid(string id)
        => Assert.Throws<UriFormatException>(() => new FeedUri(id));

    /// <summary>
    /// Ensures the <see cref="FeedUri.ToString"/> and <see cref="FeedUri.ToStringRfc"/> work correctly.
    /// </summary>
    [Fact]
    public void TestToString()
    {
        new FeedUri("http://example.com").ToStringRfc().Should().Be("http://example.com/");
        new FeedUri("http://example.com/").ToStringRfc().Should().Be("http://example.com/");
        new FeedUri("http://example.com/test1.xml").ToStringRfc().Should().Be("http://example.com/test1.xml");
        new FeedUri("https://example.com/test1.xml").ToStringRfc().Should().Be("https://example.com/test1.xml");

        new FeedUri("http://example.com/my feed.xml").ToString().Should().Be("http://example.com/my feed.xml");
        new FeedUri("http://example.com/my%20feed.xml").ToString().Should().Be("http://example.com/my feed.xml");
        new FeedUri("http://example.com/my feed.xml").ToStringRfc().Should().Be("http://example.com/my%20feed.xml");
        new FeedUri("http://example.com/my%20feed.xml").ToStringRfc().Should().Be("http://example.com/my%20feed.xml");

        var absoluteUri = new FeedUri(WindowsUtils.IsWindows ? @"C:\my feed.xml" : "/root/my feed.xml");
        absoluteUri.LocalPath.Should().Be(
            WindowsUtils.IsWindows ? @"C:\my feed.xml" : "/root/my feed.xml");
        absoluteUri.ToString().Should().Be(
            WindowsUtils.IsWindows ? @"C:\my feed.xml" : "/root/my feed.xml");
        absoluteUri.ToStringRfc().Should().Be(
            WindowsUtils.IsWindows ? @"C:\my feed.xml" : "/root/my feed.xml");

        absoluteUri = new FeedUri(WindowsUtils.IsWindows ? "file:///C:/my%20feed.xml" : "file:///root/my%20feed.xml");
        absoluteUri.LocalPath.Should().Be(
            WindowsUtils.IsWindows ? @"C:\my feed.xml" : "/root/my feed.xml");
        absoluteUri.ToString().Should().Be(
            WindowsUtils.IsWindows ? @"C:\my feed.xml" : "/root/my feed.xml");
        absoluteUri.ToStringRfc().Should().Be(
            WindowsUtils.IsWindows ? @"C:\my feed.xml" : "/root/my feed.xml");

        if (WindowsUtils.IsWindows)
        {
            absoluteUri = new FeedUri(@"\\SERVER\C$\my feed.xml");
            absoluteUri.ToString().Should().Be(
                @"\\server\C$\my feed.xml");
            absoluteUri.ToStringRfc().Should().Be(
                @"\\server\C$\my feed.xml");

            absoluteUri = new FeedUri("file://SERVER/C$/my%20feed.xml");
            absoluteUri.ToString().Should().Be(
                @"\\server\C$\my feed.xml");
            absoluteUri.ToStringRfc().Should().Be(
                @"\\server\C$\my feed.xml");
        }
    }

    [Fact]
    public void Escape()
        => FeedTest.Test1Uri.Escape().Should().Be("http%3a%2f%2fexample.com%2ftest1.xml");

    [Fact]
    public void Unescape()
        => FeedUri.Unescape("http%3a%2f%2fexample.com%2Ftest1.xml").Should().Be(FeedTest.Test1Uri);

    [Fact]
    public void PrettyEscape()
        => FeedTest.Test1Uri.PrettyEscape().Should().Be(
            // Colon is preserved on POSIX systems but not on other OSes
            UnixUtils.IsUnix ? "http:##example.com#test1.xml" : "http%3a##example.com#test1.xml");

    [Fact]
    public void PrettyUnescape()
        => FeedUri.PrettyUnescape(UnixUtils.IsUnix ? "http:##example.com#test1.xml" : "http%3a##example.com#test1.xml").Should().Be(FeedTest.Test1Uri);

    [Fact]
    public void EscapeComponent()
    {
        new FeedUri("http://example.com/foo/bar.xml").EscapeComponent().Should().Equal("http", "example.com", "foo__bar.xml");
        new FeedUri("http://example.com/").EscapeComponent().Should().Equal("http", "example.com", "");
        new FeedUri("https://example.com/").EscapeComponent().Should().Equal("https", "example.com", "");
        new FeedUri(WindowsUtils.IsWindows ? @"C:\my feed.xml" : "/root/my feed.xml").EscapeComponent().Should().Equal("file", WindowsUtils.IsWindows ? "C_3a___my_20_feed.xml" : "root__my_20_feed.xml");
        if (WindowsUtils.IsWindows)
            new FeedUri(@"\\SERVER\C$\my feed.xml").EscapeComponent().Should().Equal("file", "____server__C_24___my_20_feed.xml");
    }

    [Fact]
    public void Prefixes()
    {
        var fakeUri = new FeedUri("fake:http://example.com/");
        fakeUri.IsFake.Should().BeTrue();
        fakeUri.ToString().Should().Be("fake:http://example.com/");
        fakeUri.ToStringRfc().Should().Be("fake:http://example.com/");

        var fromDistributionUri = new FeedUri("distribution:http://example.com/");
        fromDistributionUri.IsFromDistribution.Should().BeTrue();
        fromDistributionUri.ToString().Should().Be("distribution:http://example.com/");
        fromDistributionUri.ToStringRfc().Should().Be("distribution:http://example.com/");
    }
}
