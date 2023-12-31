// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Contains test methods for <see cref="ConflictDataUtils"/>.
/// </summary>
public sealed class ConflictDataUtilsTest
{
    [Fact]
    public void NoConflicts()
    {
        var accessPointA = new MockAccessPoint {ID = "a"};
        var appEntry1 = new AppEntry
        {
            Name = "App1",
            InterfaceUri = FeedTest.Test1Uri,
            AccessPoints = new AccessPointList {Entries = {accessPointA}}
        };
        var accessPointB = new MockAccessPoint {ID = "b"};
        var appEntry2 = new AppEntry {Name = "App2", InterfaceUri = FeedTest.Test2Uri};

        var appList = new AppList {Entries = {appEntry1}};
        appList.CheckForConflicts([accessPointB], appEntry2);
    }

    [Fact]
    public void ReApply()
    {
        var accessPointA = new MockAccessPoint {ID = "a"};
        var appEntry1 = new AppEntry
        {
            Name = "App1",
            InterfaceUri = FeedTest.Test1Uri,
            AccessPoints = new AccessPointList {Entries = {accessPointA}}
        };

        var appList = new AppList {Entries = {appEntry1}};
        appList.CheckForConflicts([accessPointA], appEntry1);
    }

    [Fact]
    public void Conflict()
    {
        var accessPointA = new MockAccessPoint {ID = "a"};
        var appEntry1 = new AppEntry
        {
            Name = "App1",
            InterfaceUri = FeedTest.Test1Uri,
            AccessPoints = new AccessPointList {Entries = {accessPointA}}
        };
        var appEntry2 = new AppEntry {Name = "App2", InterfaceUri = FeedTest.Test2Uri};

        var appList = new AppList {Entries = {appEntry1}};
        Assert.Throws<ConflictException>(() => appList.CheckForConflicts([accessPointA], appEntry2));
    }

    [Fact]
    public void AccessPointCandidates()
    {
        var accessPoints = new AccessPoint[] {new MockAccessPoint {ID = "a"}, new MockAccessPoint {ID = "b"}};
        var appEntry = AppEntry();

        accessPoints.GetConflictData(appEntry).Should().Equal(new Dictionary<string, ConflictData>
        {
            {"mock:a", new ConflictData(accessPoints[0], appEntry)},
            {"mock:b", new ConflictData(accessPoints[1], appEntry)}
        });
    }

    [Fact]
    public void AccessPointCandidatesInternalConflict()
    {
        var accessPoints = new AccessPoint[] {new MockAccessPoint {ID = "a"}, new MockAccessPoint {ID = "a"}};
        var appEntry = AppEntry();

        Assert.Throws<ConflictException>(() => accessPoints.GetConflictData(appEntry));
    }

    [Fact]
    public void ExistingAppEntries()
    {
        var appList = new[]
        {
            AppEntry(new MockAccessPoint {ID = "a"}),
            AppEntry(new MockAccessPoint {ID = "b"})
        };

        appList.GetConflictData().Should().Equal(new Dictionary<string, ConflictData>
        {
            {"mock:a", new ConflictData(appList[0].AccessPoints!.Entries[0], appList[0])},
            {"mock:b", new ConflictData(appList[1].AccessPoints!.Entries[0], appList[1])}
        });
    }

    [Fact]
    public void ExistingAppEntriesInternalConflict()
    {
        var appList = new[]
        {
            AppEntry(new MockAccessPoint { ID = "a" }),
            AppEntry(new MockAccessPoint { ID = "a" })
        };

        Assert.Throws<ConflictException>(() => appList.GetConflictData());
    }

    private static AppEntry AppEntry(params AccessPoint[] accessPoints)
        => new() {InterfaceUri = FeedTest.Test1Uri, Name = "Test", AccessPoints = new() {Entries = {accessPoints}}};
}
