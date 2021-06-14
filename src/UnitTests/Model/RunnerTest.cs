// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Contains test methods for <see cref="Runner"/>.
    /// </summary>
    public class RunnerTest
    {
        /// <summary>
        /// Creates a fictive test <see cref="Runner"/>.
        /// </summary>
        private static Runner CreateTestRunner() => new()
        {
            InterfaceUri = FeedTest.Test1Uri,
            Command = "run2",
            Bindings = {EnvironmentBindingTest.CreateTestBinding()},
            Versions = new VersionRange("1.0..!2.0"),
            Arguments = {"--arg"}
        };

        /// <summary>
        /// Ensures that the class can be correctly cloned.
        /// </summary>
        [Fact]
        public void Clone()
        {
            var runner1 = CreateTestRunner();
            var runner2 = runner1.CloneRunner();

            // Ensure data stayed the same
            runner2.Should().Be(runner1, because: "Cloned objects should be equal.");
            runner2.GetHashCode().Should().Be(runner1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
            runner2.Should().NotBeSameAs(runner1, because: "Cloning should not return the same reference.");
        }
    }
}
