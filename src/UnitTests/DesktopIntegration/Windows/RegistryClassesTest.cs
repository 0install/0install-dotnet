// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Moq;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Model.Capabilities;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Windows
{
    public class RegistryClassesTest : TestWithRedirect
    {
        [Fact]
        public void TestCommandLineEscaping()
        {
            GetLaunchCommandLine(new Verb {Arguments = {"--opt", "some val", "${item}", "--opt=${item}"}})
               .Should().EndWith("--opt \"some val\" \"%V\" --opt=\"%V\"");
        }

        [Fact]
        public void TestCommandLinePrecedence()
        {
            GetLaunchCommandLine(new Verb {Arguments = {"a", "b"}, ArgumentsLiteral = "x"})
               .Should().EndWith("a b");
        }

        [Fact]
        public void TestCommandLineLiteral()
        {
            GetLaunchCommandLine(new Verb {ArgumentsLiteral = "x"})
               .Should().EndWith("x");
        }

        [Fact]
        public void TestCommandLineDefaultValue()
        {
            GetLaunchCommandLine(new Verb())
               .Should().EndWith("\"%V\"");
        }

        private static string GetLaunchCommandLine(Verb verb)
            => RegistryClasses.GetLaunchCommandLine(new FeedTarget(Fake.Feed1Uri, Fake.Feed), verb, new Mock<IIconStore>().Object, machineWide: false);
    }
}
