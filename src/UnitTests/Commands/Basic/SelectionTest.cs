// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Contains integration tests for <see cref="Selection"/>.
/// </summary>
public class SelectionTest : SelectionTestBase<Selection>
{
    [Fact] // Ensures all options are parsed and handled correctly.
    public virtual void TestNormal()
    {
        var selections = ExpectSolve();

        RunAndAssert(selections.ToXmlString(), 0, selections,
            "--xml", "http://example.com/test1.xml", "--command=command", "--os=Windows", "--cpu=i586", "--not-before=1.0", "--before=2.0", "--version-for=http://example.com/test2.xml", "2.0..!3.0");
    }

    [Fact] // Ensures local Selections XMLs are correctly detected and parsed.
    public virtual void TestImportSelections()
    {
        var selections = Fake.Selections;
        using var tempFile = new TemporaryFile("0install-test-selections");
        selections.SaveXml(tempFile);

        selections.Normalize();
        RunAndAssert(selections.ToXmlString(), 0, selections,
            "--xml", tempFile);
    }

    [Fact] // Ensures named apps are resolved to their requirements, with command-line options taking precedence.
    public void TestNamedApp()
    {
        var selections = ExpectSolve();
        new AppList
        {
            Entries =
            {
                new()
                {
                    InterfaceUri = PetName.ToUri("my app"),
                    Requirements = new(Fake.Feed1Uri, "command", new Architecture(OS.Windows, Cpu.All))
                    {
                        ExtraRestrictions =
                        {
                            {Fake.Feed1Uri, new VersionRange("0.1")},
                            {Fake.Feed2Uri, new VersionRange("2.0..!3.0")}
                        }
                    },
                    Name = "Test"
                }
            }
        }.SaveXml(AppList.GetDefaultPath());

        RunAndAssert(selections.ToXmlString(), 0, selections,
            "--xml", "--cpu=i586", "--not-before=1.0", "--before=2.0", "my app");
    }

    [Fact]
    public void TestNamedAppMissing()
        => Assert.Throws<UriFormatException>(() => Sut.Parse(["petname:missing"]));
}
