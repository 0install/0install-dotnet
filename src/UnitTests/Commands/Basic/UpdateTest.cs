// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Contains integration tests for <see cref="Update"/>.
/// </summary>
public class UpdateTest : SelectionTestBase<Update>
{
    [Fact] // Ensures local Selections XMLs are correctly detected and parsed.
    public void TestNormal()
    {
        var solver = GetMock<ISolver>();

        var requirementsOld = CreateTestRequirements();
        requirementsOld.ExtraRestrictions.Remove(Fake.Feed1Uri);
        var selectionsOld = Fake.Selections;
        solver.Setup(x => x.Solve(requirementsOld)).Returns(selectionsOld);

        var requirementsNew = CreateTestRequirements();
        var selectionsNew = Fake.Selections;
        selectionsNew.Implementations[1].Version = new("2.0");
        selectionsNew.Implementations.Add(new() {InterfaceUri = Fake.SubFeed3Uri, ID = "id3", Version = new("0.1")});
        solver.Setup(x => x.Solve(requirementsNew)).Returns(selectionsNew);

        GetMock<IFeedCache>().Setup(x => x.GetFeed(Fake.Feed1Uri)).Returns(Fake.Feed);

        // Download uncached implementations
        ExpectFetchUncached(selectionsNew,
            new() {ID = "id1", Version = new("1.0")},
            new() {ID = "id2", Version = new("1.0")},
            new() {ID = "id3", Version = new("1.0")});

        var diffNodes = new[] {new SelectionsDiffNode(Fake.Feed2Uri)};
        GetMock<ISelectionsManager>().Setup(x => x.GetDiff(selectionsOld, selectionsNew)).Returns(diffNodes);

        RunAndAssert(diffNodes, 0,
            "http://example.com/test1.xml", "--command=command", "--os=Windows", "--cpu=i586", "--not-before=1.0", "--before=2.0", "--version-for=http://example.com/test2.xml", "2.0..!3.0");
    }

    [Fact] // Ensures local Selections XMLs are rejected.
    public void TestRejectImportSelections()
    {
        var selections = Fake.Selections;
        using var tempFile = new TemporaryFile("0install-test-selections");
        selections.SaveXml(tempFile);
        Sut.Parse([tempFile]);
        Assert.Throws<NotSupportedException>(() => Sut.Execute());
    }
}
