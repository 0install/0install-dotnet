// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Contains test methods for <see cref="PetName"/>.
/// </summary>
public class PetNameTest
{
    [Theory]
    [InlineData("hello")]
    [InlineData("my app")]
    [InlineData("café")]
    [InlineData("app-1.2")]
    public void Valid(string value)
        => PetName.IsValid(value).Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData(".hidden")]
    [InlineData("a/b")]
    [InlineData(@"a\b")]
    [InlineData("a:b")]
    [InlineData("a=b")]
    [InlineData("a;b")]
    [InlineData("a'b")]
    [InlineData("a\"b")]
    [InlineData("a\nb")]
    [InlineData("0install")]
    [InlineData("0STORE")]
    public void Invalid(string value)
    {
        PetName.IsValid(value).Should().BeFalse();
        Assert.Throws<ArgumentException>(() => PetName.ToUri(value));
    }
}
