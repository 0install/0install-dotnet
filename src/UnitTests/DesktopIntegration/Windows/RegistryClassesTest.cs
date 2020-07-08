// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using NanoByte.Common.Native;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.Windows
{
    public class RegistryClassesTest : TestWithMocksAndRedirect
    {
        [SkippableFact]
        public void TestCommandLineEscaping()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "StubBuilder is only used on Windows");
            GetLaunchCommandLine(new Verb {Arguments = {"--opt", "some val", "${item}", "--opt=${item}"}})
               .Should().EndWith("--opt \"some val\" \"%V\" --opt=\"%V\"");
        }

        [SkippableFact]
        public void TestCommandLinePrecedence()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "StubBuilder is only used on Windows");
            GetLaunchCommandLine(new Verb {Arguments = {"a", "b"}, ArgumentsLiteral = "x"})
               .Should().EndWith("a b");
        }

        [SkippableFact]
        public void TestCommandLineLiteral()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "StubBuilder is only used on Windows");
            GetLaunchCommandLine(new Verb {ArgumentsLiteral = "x"})
               .Should().EndWith("x");
        }

        [SkippableFact]
        public void TestCommandLineDefaultValue()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "StubBuilder is only used on Windows");
            GetLaunchCommandLine(new Verb())
               .Should().EndWith("\"%1\"");
        }

        private string GetLaunchCommandLine(Verb verb)
            => RegistryClasses.GetLaunchCommandLine(new FeedTarget(Fake.Feed1Uri, Fake.Feed), verb, CreateMock<IIconStore>().Object, machineWide: false);
    }
}
