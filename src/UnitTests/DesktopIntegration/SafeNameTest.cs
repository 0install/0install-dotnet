// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Contains test methods for <see cref="SafeName"/>.
/// </summary>
public class SafeNameTest
{
    public static TheoryData<string> ValidNames =>
    [
        "hello",
        "my app",
        "café",
        "app-1.2",
        "app-",
        "console"
    ];

    public static TheoryData<string> InvalidNames =>
    [
        "",
        ".",
        "..",
        ".hidden",
        "a/b",
        @"a\b",
        "a:b",
        "a=b",
        "a;b",
        "a'b",
        "a\"b",
        "a\nb",
        "0install",
        "0STORE",
        "-x",
        " ",
        " foo",
        "foo ",
        "foo.",
        "a*b",
        "a?b",
        "a<b",
        "a>b",
        "a|b",
        "NUL",
        "con",
        "COM1.app"
    ];

    [Theory]
    [MemberData(nameof(ValidNames))]
    public void Valid(string value)
    {
        SafeName.IsValid(value).Should().BeTrue();
        SafeName.Validate(value);
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Invalid(string value)
    {
        SafeName.IsValid(value).Should().BeFalse();
        Assert.Throws<IOException>(() => SafeName.Validate(value));
    }

    public static TheoryData<string> InvalidPaths =>
    [
        ".",
        "..",
        "../a",
        "a/..",
        "a/../b",
        @"a\..\..\b",
        "a/./b",
        "a/.. /b",
        "/",
        "/a",
        @"\a",
        "a/",
        "a//b",
        @"a/\b",
        @"\\server\share",
        "//server/share",
        "C:/a",
        "C:a",
        "a/C:",
        "a/b?",
        "a/ b",
        "a/b.",
        "a/-b",
        "a/b;c",
        "a/NUL",
        "a/0install"
    ];

    [Theory]
    [InlineData("a")]
    [InlineData("a/b")]
    [InlineData(@"a\b")]
    [InlineData("My Category/My App/c")]
    public void ValidPath(string value)
        => SafeName.ValidatePath(value);

    [Theory]
    [InlineData("")]
    [MemberData(nameof(InvalidPaths))]
    public void InvalidPath(string value)
        => Assert.Throws<IOException>(() => SafeName.ValidatePath(value));
}
