// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using NanoByte.Common.Native;
using ZeroInstall.Model.Capabilities;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.Windows;

[SupportedOSPlatform("windows")]
public class RegistryClassesTest : TestWithRedirect
{
    public RegistryClassesTest()
    {
        Skip.IfNot(WindowsUtils.IsWindows, "Registry access is only available on Windows");
    }

    [SkippableFact]
    public void CommandLineEscaping()
    {
        GetLaunchCommandLine(new() {Name = Verb.NameOpen, Arguments = {"--opt", "some val", "${item}", "--opt=${item}"}})
           .Should().EndWith("""
               --opt "some val" "%V" --opt="%V"
               """);
    }

    [SkippableFact]
    public void CommandLinePrecedence()
    {
        GetLaunchCommandLine(new() {Name = Verb.NameOpen, Arguments = {"a", "b"}, ArgumentsLiteral = "x"})
           .Should().EndWith("a b");
    }

    [SkippableFact]
    public void CommandLineLiteral()
    {
        GetLaunchCommandLine(new() {Name = Verb.NameOpen, ArgumentsLiteral = "x"})
           .Should().EndWith("x");
    }

    [SkippableFact]
    public void CommandLineDefaultValue()
    {
        GetLaunchCommandLine(new() {Name = Verb.NameOpen})
           .Should().EndWith("\"%V\"");
    }

    private static string GetLaunchCommandLine(Verb verb)
        => RegistryClasses.GetLaunchCommandLine(new FeedTarget(Fake.Feed1Uri, Fake.Feed), verb, Mock.Of<IIconStore>(), machineWide: false);
}
