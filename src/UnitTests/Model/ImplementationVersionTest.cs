// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="ImplementationVersion"/>.
/// </summary>
public class ImplementationVersionTest
{
    /// <summary>
    /// Ensures the <see cref="ImplementationVersion.TryCreate"/> correctly parses valid strings.
    /// </summary>
    [Theory]
    [InlineData("0.1"), InlineData("1"), InlineData("1.0"), InlineData("1.1"), InlineData("1.1-"), InlineData("1.2-pre"), InlineData("1.2-pre1"), InlineData("1.2-rc1"), InlineData("1.2"), InlineData("1.2-0"), InlineData("1.2--0"), InlineData("1.2-post"), InlineData("1.2-post1-pre"), InlineData("1.2-post1"), InlineData("1.2.1-pre"), InlineData("1.2.1.4"), InlineData("1.2.3"), InlineData("1.2.10"), InlineData("3")]
    public void TestTryCreateValid(string version)
    {
        ImplementationVersion.TryCreate(version, out var result).Should().BeTrue();
        result!.ToString().Should().Be(version);
    }

    /// <summary>
    /// Ensures the <see cref="ImplementationVersion.TryCreate"/> correctly rejects invalid strings.
    /// </summary>
    [Theory]
    [InlineData(""), InlineData("a"), InlineData("pre-1"), InlineData("1.0-1post")]
    public void TestTryCreateInvalid(string version)
        => ImplementationVersion.TryCreate(version, out _).Should().BeFalse();

    /// <summary>
    /// Ensures the constructor correctly parses <see cref="string"/>s and <see cref="Version"/>s.
    /// </summary>
    [Fact]
    public void VersionConstructor()
    {
        new ImplementationVersion(new Version(1, 2)).Should().Be(new ImplementationVersion("1.2"));
        new ImplementationVersion(new Version(1, 2, 3)).Should().Be(new ImplementationVersion("1.2.3"));
        new ImplementationVersion(new Version(1, 2, 3, 4)).Should().Be(new ImplementationVersion("1.2.3.4"));
    }

    /// <summary>
    /// Ensures the <see cref="Version"/> constructor correctly handles template variables.
    /// </summary>
    [Fact]
    public void TemplateVariable()
    {
        var version = new ImplementationVersion("1-pre{var}");
        version.ContainsTemplateVariables.Should().BeTrue();
        version.ToString().Should().Be("1-pre{var}");

        version = new ImplementationVersion("{var}");
        version.ContainsTemplateVariables.Should().BeTrue();
        version.ToString().Should().Be("{var}");
    }

    [Theory]
    [InlineData("1"), InlineData("1.2"), InlineData("1.2-0"), InlineData("1.2-post")]
    public void Equal(string value)
    {
        var version = new ImplementationVersion(value);
        version.Should().Be(version);
        version.CompareTo(version).Should().Be(0);
    }

    [Theory]
    [InlineData("1.2-post-3", "1.2-pre-3")]
    [InlineData("1.2-pre-4", "1.2-pre-3")]
    [InlineData("1.2-pre--3", "1.2-pre-3")]
    [InlineData("1.2-pre", "1.2-pre-3")]
    public void NotEqual(string left, string right)
    {
        var leftVersion = new ImplementationVersion(left);
        var rightVersion = new ImplementationVersion(right);
        leftVersion.Should().NotBe(rightVersion);
        leftVersion.CompareTo(rightVersion).Should().NotBe(0);
    }

    [Theory]
    [InlineData("1", "2")]
    [InlineData("1.2", "1.2-0")]
    [InlineData("1.2", "1.2-post")]
    public void CompareTo(string smaller, string larger)
    {
        var smallerVersion = new ImplementationVersion(smaller);
        var largerVersion = new ImplementationVersion(larger);

        smallerVersion.Should().BeLessThan(largerVersion);
        smallerVersion.CompareTo(largerVersion).Should().BeNegative();

        largerVersion.Should().BeGreaterThan(smallerVersion);
        largerVersion.CompareTo(smallerVersion).Should().BePositive();
    }

    [Fact]
    public void CompareToNull()
    {
        new ImplementationVersion("1").Should().BeGreaterThan(null);
        new ImplementationVersion("1").CompareTo(null).Should().BePositive();
    }
}
