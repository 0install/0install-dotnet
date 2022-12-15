// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using ZeroInstall.Model.Capabilities;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.Windows;

[SupportedOSPlatform("windows")]
public class RegistryClassesTest : TestWithRedirect
{
    [Fact]
    public void CommandLineEscaping()
    {
        GetLaunchCommandLine(new Verb {Name = Verb.NameOpen, Arguments = {"--opt", "some val", "${item}", "--opt=${item}"}})
           .Should().EndWith("""
               --opt "some val" "%V" --opt="%V"
               """);
    }

    [Fact]
    public void CommandLinePrecedence()
    {
        GetLaunchCommandLine(new Verb {Name = Verb.NameOpen, Arguments = {"a", "b"}, ArgumentsLiteral = "x"})
           .Should().EndWith("a b");
    }

    [Fact]
    public void CommandLineLiteral()
    {
        GetLaunchCommandLine(new Verb {Name = Verb.NameOpen, ArgumentsLiteral = "x"})
           .Should().EndWith("x");
    }

    [Fact]
    public void CommandLineDefaultValue()
    {
        GetLaunchCommandLine(new Verb {Name = Verb.NameOpen})
           .Should().EndWith("\"%V\"");
    }

    private static string GetLaunchCommandLine(Verb verb)
        => RegistryClasses.GetLaunchCommandLine(new FeedTarget(Fake.Feed1Uri, Fake.Feed), verb, Mock.Of<IIconStore>(), machineWide: false);
}
