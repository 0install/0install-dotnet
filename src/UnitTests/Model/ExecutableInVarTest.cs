// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="ExecutableInVar"/>.
/// </summary>
public class ExecutableInVarTest
{
    /// <summary>
    /// Creates a fictive test <see cref="ExecutableInVar"/>.
    /// </summary>
    internal static ExecutableInVar CreateTestBinding() => new() {Name = "name", Command = "command"};

    /// <summary>
    /// Ensures that the class can be correctly cloned.
    /// </summary>
    [Fact]
    public void Clone()
    {
        var binding1 = CreateTestBinding();
        var binding2 = binding1.Clone();

        // Ensure data stayed the same
        binding2.Should().Be(binding1, because: "Cloned objects should be equal.");
        binding2.GetHashCode().Should().Be(binding1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
        binding2.Should().NotBeSameAs(binding1, because: "Cloning should not return the same reference.");
    }
}
