// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Contains test methods for <see cref="PackageImplementation"/>.
    /// </summary>
    public class PackageImplementationTest
    {
        /// <summary>
        /// Creates a fictive test <see cref="PackageImplementation"/>.
        /// </summary>
        internal static PackageImplementation CreateTestImplementation() => new PackageImplementation
        {
            Distributions = {"RPM"},
            Version = new ImplementationVersion("1.0"),
            Architecture = new Architecture(OS.Windows, Cpu.I586),
            Languages = {"en-US"},
            Main = "executable",
            DocDir = "doc",
            Stability = Stability.Developer,
            Bindings = {EnvironmentBindingTest.CreateTestBinding()}
        };

        /// <summary>
        /// Ensures that the class can be correctly cloned.
        /// </summary>
        [Fact]
        public void TestClone()
        {
            var implementation1 = CreateTestImplementation();
            var implementation2 = implementation1.CloneImplementation();

            // Ensure data stayed the same
            implementation2.Should().Be(implementation1, because: "Cloned objects should be equal.");
            implementation2.GetHashCode().Should().Be(implementation1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
            implementation2.Should().NotBeSameAs(implementation1, because: "Cloning should not return the same reference.");
        }
    }
}
