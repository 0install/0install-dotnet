// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

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
}
