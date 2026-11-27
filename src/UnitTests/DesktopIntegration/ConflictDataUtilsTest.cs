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
            AccessPoints = new() {Entries = {accessPointA}}
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
            AccessPoints = new() {Entries = {accessPointA}}
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
            AccessPoints = new() {Entries = {accessPointA}}
        };
        var appEntry2 = new AppEntry {Name = "App2", InterfaceUri = FeedTest.Test2Uri};

        var appList = new AppList {Entries = {appEntry1}};
        Assert.Throws<ConflictException>(() => appList.CheckForConflicts([accessPointA], appEntry2))
              .Message.Should().Contain($"{accessPointA} in App1").And.Contain($"{accessPointA} in App2");
    }

    [Fact]
    public void ConflictMenuEntryCategorySeparators()
    {
        var appEntry1 = new AppEntry
        {
            Name = "App1",
            InterfaceUri = FeedTest.Test1Uri,
            AccessPoints = new() {Entries = {new MenuEntry {Category = "a/b", Name = "Entry"}}}
        };
        var appEntry2 = new AppEntry {Name = "App2", InterfaceUri = FeedTest.Test2Uri};

        var appList = new AppList {Entries = {appEntry1}};
        Assert.Throws<ConflictException>(() => appList.CheckForConflicts([new MenuEntry {Category = @"a\b", Name = "Entry"}], appEntry2));
    }

    [Fact]
    public void ConflictMenuEntryCaseInsensitive()
    {
        var appEntry1 = new AppEntry
        {
            Name = "App1",
            InterfaceUri = FeedTest.Test1Uri,
            AccessPoints = new() {Entries = {new MenuEntry {Category = "Games", Name = "Entry"}}}
        };
        var appEntry2 = new AppEntry {Name = "App2", InterfaceUri = FeedTest.Test2Uri};

        var appList = new AppList {Entries = {appEntry1}};
        Assert.Throws<ConflictException>(() => appList.CheckForConflicts([new MenuEntry {Category = "games", Name = "ENTRY"}], appEntry2));
    }

    [Fact]
    public void AccessPointCandidatesInternalConflict()
    {
        var accessPoints = new AccessPoint[] {new MockAccessPoint {ID = "a"}, new MockAccessPoint {ID = "a"}};
        var appEntry = AppEntry();

        Assert.Throws<ConflictException>(() => accessPoints.GetConflictData(appEntry));
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
