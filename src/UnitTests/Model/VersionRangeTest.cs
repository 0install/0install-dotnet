// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="VersionRange"/>.
/// </summary>
public class VersionRangeTest
{
    /// <summary>
    /// Ensures <see cref="VersionRange"/> are correctly parsed from strings.
    /// </summary>
    [Fact]
    public void Parse()
    {
        new VersionRange("2.6").Parts.Should().Equal(new VersionRangePartExact(new("2.6")));
        new VersionRange("..!3").Parts.Should().Equal(new VersionRangePartRange(LowerInclusive: null, UpperExclusive: new("3")));
        new VersionRange("!3").Parts.Should().Equal(new VersionRangePartExclude(new("3")));
        new VersionRange("2.6..!3 | 3.2.2..").Parts.Should().Equal(
            new VersionRangePartRange(LowerInclusive: new("2.6"), UpperExclusive: new("3")),
            new VersionRangePartRange(LowerInclusive: new("3.2.2"), UpperExclusive: null));
        new VersionRange("..!3 | 3.2.2..").Parts.Should().Equal(
            new VersionRangePartRange(LowerInclusive: null, UpperExclusive: new("3")),
            new VersionRangePartRange(LowerInclusive: new("3.2.2"), UpperExclusive: null));
    }

    /// <summary>
    /// Ensures <see cref="VersionRange"/> objects are correctly compared.
    /// </summary>
    [Fact]
    public void Equality()
    {
        new VersionRange("2.6").Should().Be(new VersionRange("2.6"));
        new VersionRange("..!3").Should().Be(new VersionRange("..!3"));
        new VersionRange("!3").Should().Be(new VersionRange("!3"));
        new VersionRange("2.6..!3 | 3.2.2..").Should().Be(new VersionRange("2.6..!3|3.2.2.."));
        new VersionRange("..!3 | 3.2.2..").Should().Be(new VersionRange("..!3|3.2.2.."));
        new VersionRange("2.6..!3|3.2.2..!3.3").Should().NotBe(new VersionRange("2.6..!3|3.2.2.."));
        new VersionRange("2.6..|3.2.2..").Should().NotBe(new VersionRange("2.6..!3|3.2.2.."));
        new VersionRange("..!3|3.2.2..").Should().NotBe(new VersionRange("2.6..!3|3.2.2.."));
    }

    /// <summary>
    /// Ensures <see cref="VersionRange.ToString"/> correctly serializes ranges.
    /// </summary>
    [Fact]
    public void TestToString()
    {
        new VersionRange("2.6").ToString().Should().Be("2.6");
        new VersionRange("..!3").ToString().Should().Be("..!3");
        new VersionRange("!3").ToString().Should().Be("!3");
        new VersionRange("2.6..!3 | 3.2.2..").ToString().Should().Be("2.6..!3|3.2.2..");
        new VersionRange("..!3 | 3.2.2..").ToString().Should().Be("..!3|3.2.2..");
        new VersionRange("2.6..!3|3.2.2..!3.3").ToString().Should().NotBe("2.6..!3|3.2.2..");
        new VersionRange("2.6..|3.2.2..").ToString().Should().NotBe("2.6..!3|3.2.2..");
        new VersionRange("..!3|3.2.2..").ToString().Should().NotBe("2.6..!3|3.2.2..");
    }

    /// <summary>
    /// Ensures <see cref="VersionRange.Intersect"/> works correctly.
    /// </summary>
    [Fact]
    public void Intersect()
    {
        IntersectShouldBe(VersionRange.None, new VersionRange("..!2"), expected: VersionRange.None);
        IntersectShouldBe(VersionRange.None, new VersionRange("1.."), expected: VersionRange.None);
        IntersectShouldBe(VersionRange.None, new VersionRange("1..!2"), expected: VersionRange.None);

        IntersectShouldBe(new VersionRange(), new VersionRange("1"), expected: new VersionRange("1"));
        IntersectShouldBe(new VersionRange(), new VersionRange("!1"), expected: new VersionRange("!1"));
        IntersectShouldBe(new VersionRange(), new VersionRange("1.."), expected: new VersionRange("1.."));
        IntersectShouldBe(new VersionRange(), new VersionRange("..!2"), expected: new VersionRange("..!2"));
        IntersectShouldBe(new VersionRange(), new VersionRange("1..!2"), expected: new VersionRange("1..!2"));

        IntersectShouldBe(new VersionRange("1.."), new VersionRange("..!3"), expected: new VersionRange("1..!3"));
        IntersectShouldBe(new VersionRange("1.."), new VersionRange("2..!3"), expected: new VersionRange("2..!3"));
        IntersectShouldBe(new VersionRange("1.."), new VersionRange("1"), expected: new VersionRange("1"));
        IntersectShouldBe(new VersionRange("1.."), new VersionRange("0"), expected: VersionRange.None);
        IntersectShouldBe(new VersionRange("1.."), new VersionRange("!0"), expected: new VersionRange("1.."));
        IntersectNotSupported(new VersionRange("1.."), new VersionRange("!1"));
        IntersectNotSupported(new VersionRange("1.."), new VersionRange("!1.5"));

        IntersectShouldBe(new VersionRange("..!2"), new VersionRange("..!3"), expected: new VersionRange("..!2"));
        IntersectShouldBe(new VersionRange("..!2"), new VersionRange("2..!3"), expected: VersionRange.None);
        IntersectShouldBe(new VersionRange("..!2"), new VersionRange("1"), expected: new VersionRange("1"));
        IntersectShouldBe(new VersionRange("..!2"), new VersionRange("3"), expected: VersionRange.None);
        IntersectShouldBe(new VersionRange("..!2"), new VersionRange("2"), expected: VersionRange.None);
        IntersectShouldBe(new VersionRange("..!2"), new VersionRange("!3"), expected: new VersionRange("..!2"));
        IntersectShouldBe(new VersionRange("..!2"), new VersionRange("!2"), expected: new VersionRange("..!2"));
        IntersectNotSupported(new VersionRange("..!2"), new VersionRange("!1"));
        IntersectNotSupported(new VersionRange("..!2"), new VersionRange("!1.5"));

        IntersectShouldBe(new VersionRange("1..!2"), new VersionRange("..!3"), expected: new VersionRange("1..!2"));
        IntersectShouldBe(new VersionRange("1..!2"), new VersionRange("2..!3"), expected: VersionRange.None);
        IntersectShouldBe(new VersionRange("1..!2"), new VersionRange("1"), expected: new VersionRange("1"));
        IntersectShouldBe(new VersionRange("1..!2"), new VersionRange("3"), expected: VersionRange.None);
        IntersectShouldBe(new VersionRange("1..!2"), new VersionRange("2"), expected: VersionRange.None);
        IntersectShouldBe(new VersionRange("1..!2"), new VersionRange("0"), expected: VersionRange.None);
        IntersectShouldBe(new VersionRange("1..!2"), new VersionRange("!3"), expected: new VersionRange("1..!2"));
        IntersectShouldBe(new VersionRange("1..!2"), new VersionRange("!2"), expected: new VersionRange("1..!2"));
        IntersectShouldBe(new VersionRange("1..!2"), new VersionRange("!0"), expected: new VersionRange("1..!2"));
        IntersectNotSupported(new VersionRange("1..!2"), new VersionRange("!1"));
        IntersectNotSupported(new VersionRange("1..!2"), new VersionRange("!1.5"));

        IntersectShouldBe(new VersionRange("1"), new VersionRange("!1"), expected: VersionRange.None);
        IntersectShouldBe(new VersionRange("1"), new VersionRange("!2"), expected: new VersionRange("1"));
        IntersectShouldBe(new VersionRange("1|2"), new VersionRange("2|3"), expected: new VersionRange("2"));
        IntersectShouldBe(new VersionRange("1|2"), new VersionRange("!2"), expected: new VersionRange("1"));
        IntersectShouldBe(new VersionRange("..!2|3.."), new VersionRange("..!2.5|3.5.."), expected: new VersionRange("..!2|3.5.."));
        IntersectShouldBe(new VersionRange("1..!2|3..!4"), new VersionRange("1.5..!2.5|3.5..!4.5"), expected: new VersionRange("1.5..!2|3.5..!4"));
    }

    private static void IntersectShouldBe(VersionRange a, VersionRange b, VersionRange expected)
    {
        a.Intersect(b).Should().Be(expected);
        b.Intersect(a).Should().Be(expected);
    }

    private static void IntersectNotSupported(VersionRange a, VersionRange b)
    {
        Assert.Throws<NotSupportedException>(() => a.Intersect(b));
        Assert.Throws<NotSupportedException>(() => b.Intersect(a));
    }

    /// <summary>
    /// Ensures <see cref="VersionRange.Match"/> works correctly.
    /// </summary>
    [Fact]
    public void Match()
    {
        new VersionRange("1.2").Match(new("1.2")).Should().BeTrue();
        new VersionRange("1.2").Match(new("1.3")).Should().BeFalse();
        new VersionRange("!1.2").Match(new("1.3")).Should().BeTrue();
        new VersionRange("!1.2").Match(new("1.2")).Should().BeFalse();
        new VersionRange("1.2..").Match(new("1.2")).Should().BeTrue();
        new VersionRange("1.2..").Match(new("1.1")).Should().BeFalse();
        new VersionRange("..!1.2").Match(new("1.1")).Should().BeTrue();
        new VersionRange("..!1.2").Match(new("1.2")).Should().BeFalse();
        new VersionRange("1.0..!1.2").Match(new("1.0")).Should().BeTrue();
        new VersionRange("1.0..!1.2").Match(new("1.1")).Should().BeTrue();
        new VersionRange("1.0..!1.2").Match(new("0.9")).Should().BeFalse();
        new VersionRange("1.0..!1.2").Match(new("1.2")).Should().BeFalse();
    }
}
