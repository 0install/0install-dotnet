// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="Restriction"/>.
/// </summary>
public class RestrictionTest
{
    /// <summary>
    /// Creates a fictive test <see cref="Restriction"/>.
    /// </summary>
    public static Restriction CreateTestRestriction() => new()
    {
        InterfaceUri = FeedTest.Test1Uri,
        Versions = new VersionRange("1.0..!2.0"),
        OS = OS.Windows,
        Distributions = {Restriction.DistributionZeroInstall},
        Constraints = {new Constraint {NotBefore = new("1.0"), Before = new("2.0")}}
    };

    /// <summary>
    /// Ensures that the class can be correctly cloned and compared.
    /// </summary>
    [Fact]
    public void CloneEquals()
    {
        var restriction1 = CreateTestRestriction();
        restriction1.Should().Be(restriction1, because: "Equals() should be reflexive.");
        restriction1.GetHashCode().Should().Be(restriction1.GetHashCode(), because: "GetHashCode() should be reflexive.");

        var restriction2 = restriction1.Clone();
        restriction2.Should().Be(restriction1, because: "Cloned objects should be equal.");
        restriction2.GetHashCode().Should().Be(restriction1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
        restriction2.Should().NotBeSameAs(restriction1, because: "Cloning should not return the same reference.");

        restriction2.Constraints.Add(new Constraint());
        restriction2.Should().NotBe(restriction1, because: "Modified objects should no longer be equal");
    }

    /// <summary>
    /// Ensures <see cref="Restriction.Normalize"/> correctly converts <see cref="Constraint.NotBefore"/> to <see cref="Restriction.Versions"/>.
    /// </summary>
    [Fact]
    public void NormalizeNotBefore()
    {
        var restriction = new Restriction {InterfaceUri = FeedTest.Test1Uri, Constraints = {new Constraint {NotBefore = new("1.0")}}};
        restriction.Normalize();
        restriction.Versions.Should().Be(new VersionRange("1.0.."));
    }

    /// <summary>
    /// Ensures <see cref="Restriction.Normalize"/> correctly converts <see cref="Constraint.Before"/> to <see cref="Restriction.Versions"/>.
    /// </summary>
    [Fact]
    public void NormalizeBefore()
    {
        var restriction = new Restriction {InterfaceUri = FeedTest.Test1Uri, Constraints = {new Constraint {Before = new("2.0")}}};
        restriction.Normalize();
        restriction.Versions.Should().Be(new VersionRange("..!2.0"));
    }

    /// <summary>
    /// Ensures <see cref="Restriction.Normalize"/> correctly converts <see cref="Constraint.NotBefore"/> and <see cref="Constraint.Before"/> to <see cref="Restriction.Versions"/>.
    /// </summary>
    [Fact]
    public void NormalizeRange()
    {
        var restriction = new Restriction {InterfaceUri = FeedTest.Test1Uri, Constraints = {new Constraint {NotBefore = new("1.0"), Before = new("2.0")}}};
        restriction.Normalize();
        restriction.Versions.Should().Be(new VersionRange("1.0..!2.0"));
    }

    /// <summary>
    /// Ensures <see cref="Restriction.Normalize"/> deduces correct <see cref="Restriction.Versions"/> values from overlapping <see cref="Restriction.Constraints"/>.
    /// </summary>
    [Fact]
    public void NormalizeOverlap()
    {
        var restriction = new Restriction
        {
            InterfaceUri = FeedTest.Test1Uri,
            Constraints =
            {
                new Constraint {NotBefore = new("1.0"), Before = new("2.0")},
                new Constraint {NotBefore = new("0.9"), Before = new("1.9")}
            }
        };
        restriction.Normalize();
        restriction.Versions.Should().Be(new VersionRange("1.0..!1.9"));
    }
}
