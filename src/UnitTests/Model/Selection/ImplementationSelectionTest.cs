// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;

namespace ZeroInstall.Model.Selection
{
    /// <summary>
    /// Contains test methods for <see cref="ImplementationSelection"/>.
    /// </summary>
    public class ImplementationSelectionTest
    {
        /// <summary>
        /// Creates a fictive test <see cref="ImplementationSelection"/>.
        /// </summary>
        internal static ImplementationSelection CreateTestImplementation1() => new()
        {
            InterfaceUri = FeedTest.Test1Uri,
            FromFeed = FeedTest.Sub1Uri,
            ID = "id1",
            ManifestDigest = new ManifestDigest(sha256: "123"),
            Version = new("1.0"),
            Architecture = new(OS.Windows, Cpu.I586),
            Languages = {"en-US"},
            DocDir = "doc",
            Stability = Stability.Developer,
            Bindings =
            {
                new EnvironmentBinding {Name = "TEST1_PATH_SELF", Default = "default", Mode = EnvironmentMode.Append},
                new EnvironmentBinding {Name = "TEST1_VALUE", Value = "test1", Mode = EnvironmentMode.Replace},
                new EnvironmentBinding {Name = "TEST1_EMPTY", Value = "", Mode = EnvironmentMode.Append}
            },
            Dependencies =
            {
                new Dependency
                {
                    InterfaceUri = FeedTest.Test2Uri,
                    Bindings = {new EnvironmentBinding {Name = "TEST2_PATH_SUB_DEP", Insert = "sub", Default = "default", Mode = EnvironmentMode.Append}}
                }
            },
            Commands = {CommandTest.CreateTestCommand1(), CommandTest.CreateTestCommand1Test()}
        };

        /// <summary>
        /// Creates a fictive test <see cref="ImplementationSelection"/>.
        /// </summary>
        internal static ImplementationSelection CreateTestImplementation2() => new()
        {
            InterfaceUri = FeedTest.Test2Uri,
            FromFeed = FeedTest.Sub2Uri,
            ID = "id2",
            ManifestDigest = new ManifestDigest(sha256: "abc"),
            Version = new("1.0"),
            Architecture = new(OS.Windows, Cpu.I586),
            Languages = {"en-US"},
            DocDir = "doc",
            Stability = Stability.Developer,
            Bindings =
            {
                new EnvironmentBinding {Name = "TEST2_PATH_SELF", Default = "default", Mode = EnvironmentMode.Prepend},
                new EnvironmentBinding {Name = "TEST2_VALUE", Value = "test2", Mode = EnvironmentMode.Replace}
            },
            Dependencies =
            {
                new Dependency
                {
                    InterfaceUri = FeedTest.Test1Uri,
                    Bindings =
                    {
                        new ExecutableInVar {Name = "exec-in-var", Command = Command.NameTest},
                        new ExecutableInPath {Name = "exec-in-path", Command = Command.NameTest}
                    }
                }
            },
            Commands = {CommandTest.CreateTestCommand2()}
        };

        /// <summary>
        /// Ensures that the class can be correctly cloned.
        /// </summary>
        [Fact]
        public void Clone()
        {
            var implementation1 = CreateTestImplementation1();
            var implementation2 = implementation1.Clone();

            // Ensure data stayed the same
            implementation2.Should().Be(implementation1, because: "Cloned objects should be equal.");
            implementation2.GetHashCode().Should().Be(implementation1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
            implementation2.Should().NotBeSameAs(implementation1, because: "Cloning should not return the same reference.");
        }
    }
}
