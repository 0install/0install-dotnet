// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Contains test methods for <see cref="EnvironmentBinding"/>.
    /// </summary>
    public class EnvironmentBindingTest
    {
        /// <summary>
        /// Creates a fictive test <see cref="EnvironmentBinding"/>.
        /// </summary>
        internal static EnvironmentBinding CreateTestBinding() => new EnvironmentBinding
        {
            Name = "name",
            Value = "value",
            Default = "default",
            Mode = EnvironmentMode.Replace,
            Separator = ","
        };

        /// <summary>
        /// Ensures that the class can be correctly cloned.
        /// </summary>
        [Fact]
        public void TestClone()
        {
            var binding1 = CreateTestBinding();
            var binding2 = binding1.Clone();

            // Ensure data stayed the same
            binding2.Should().Be(binding1, because: "Cloned objects should be equal.");
            binding2.GetHashCode().Should().Be(binding1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
            binding2.Should().NotBeSameAs(binding1, because: "Cloning should not return the same reference.");
        }
    }
}
