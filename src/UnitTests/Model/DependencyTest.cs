// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="Dependency"/>.
/// </summary>
public class DependencyTest
{
    /// <summary>
    /// Creates a fictive test <see cref="Dependency"/>.
    /// </summary>
    public static Dependency CreateTestDependency() => new()
    {
        InterfaceUri = FeedTest.Test1Uri,
        Versions = new VersionRange("1.0..!2.0"),
        OS = OS.Windows,
        Distributions = {Restriction.DistributionZeroInstall},
        Constraints = {new Constraint {NotBefore = new("1.0"), Before = new("2.0")}},
        Importance = Importance.Recommended
    };

    /// <summary>
    /// Ensures that the class can be correctly cloned and compared.
    /// </summary>
    [Fact]
    public void CloneEquals()
    {
        var dependency1 = CreateTestDependency();
        dependency1.Should().Be(dependency1, because: "Equals() should be reflexive.");
        dependency1.GetHashCode().Should().Be(dependency1.GetHashCode(), because: "GetHashCode() should be reflexive.");

        var dependency2 = ((ICloneable<Dependency>)dependency1).Clone();
        dependency2.Should().Be(dependency1, because: "Cloned objects should be equal.");
        dependency2.GetHashCode().Should().Be(dependency1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
        ReferenceEquals(dependency1, dependency2).Should().BeFalse(because: "Cloning should not return the same reference.");

        dependency2.Bindings.Add(new EnvironmentBinding {Name = "dummy"});
        dependency2.Should().NotBe(dependency1, because: "Modified objects should no longer be equal");
    }
}
