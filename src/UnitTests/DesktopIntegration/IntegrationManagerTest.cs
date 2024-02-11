// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Model.Capabilities;
using ZeroInstall.Services;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Contains test methods for <see cref="IntegrationManager"/>.
/// </summary>
[Collection("Desktop integration")]
public sealed class IntegrationManagerTest : TestWithRedirect
{
    private readonly IntegrationManager _integrationManager;

    public IntegrationManagerTest()
    {
        _integrationManager = new IntegrationManager(new Config(), new MockTaskHandler());
    }

    public override void Dispose()
    {
        _integrationManager.Dispose();
        base.Dispose();
    }

    [Fact]
    public void AddApp()
    {
        var capabilityList = CapabilityListTest.CreateTestCapabilityList();
        var target = new FeedTarget(FeedTest.Test1Uri, new Feed {Name = "Test", CapabilityLists = {capabilityList}});
        _integrationManager.AddApp(target);

        _integrationManager.AppList.Entries
                           .Should().Equal(new AppEntry {InterfaceUri = FeedTest.Test1Uri, Name = target.Feed.Name, CapabilityLists = {capabilityList}});

        _integrationManager.Invoking(x => x.AddApp(target))
                           .Should().Throw<InvalidOperationException>(because: "Do not allow adding applications to AppList more than once.");
    }

    [Fact]
    public void RemoveApp()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, new Feed {Name = "Test"});
        var appEntry = _integrationManager.AddApp(target);

        // Inject access point into AppEntry (without running integration)
        using var unapplyFlag = new TemporaryFlagFile("0install-test-flag");
        appEntry.AccessPoints = new() {Entries = {new MockAccessPoint {UnapplyFlagPath = unapplyFlag}}};

        _integrationManager.RemoveApp(appEntry);
        _integrationManager.AppList.Entries.Should().BeEmpty();

        unapplyFlag.Set.Should().BeTrue(because: "Access points should be unapplied when their AppEntry is removed");
        _integrationManager.Invoking(x => x.RemoveApp(appEntry))
                           .Should().NotThrow(because: "Allow multiple removals of applications.");
    }

    [Fact]
    public void AddAccessPoints()
    {
        var capabilityList = CapabilityListTest.CreateTestCapabilityList();
        var feed1 = new Feed {Name = "Test", CapabilityLists = {capabilityList}};
        var feed2 = new Feed {Name = "Test", CapabilityLists = {capabilityList}};
        using var applyFlag1 = new TemporaryFlagFile("0install-test-flag");
        using var applyFlag2 = new TemporaryFlagFile("0install-test-flag");
        var accessPoints1 = new AccessPoint[] {new MockAccessPoint {ID = "id1", Capability = "my_ext1", ApplyFlagPath = applyFlag1}};
        var accessPoints2 = new AccessPoint[] {new MockAccessPoint {ID = "id2", Capability = "my_ext2", ApplyFlagPath = applyFlag2}};

        _integrationManager.AppList.Entries.Count.Should().Be(0);
        var appEntry1 = _integrationManager.AddApp(new FeedTarget(FeedTest.Test1Uri, feed1));
        _integrationManager.AddAccessPoints(appEntry1, feed1, accessPoints1);
        _integrationManager.AppList.Entries.Count.Should().Be(1, because: "Should implicitly create missing AppEntries");
        applyFlag1.Set.Should().BeTrue(because: "Should apply AccessPoint");
        applyFlag1.Set = false;

        _integrationManager.Invoking(x => x.AddAccessPoints(appEntry1, feed1, accessPoints1))
                           .Should().NotThrow(because: "Duplicate access points should be silently reapplied");
        applyFlag1.Set.Should().BeTrue(because: "Duplicate access points should be silently reapplied");

        _integrationManager.AddAccessPoints(appEntry1, feed1, accessPoints2);
        applyFlag2.Set = false;

        var appEntry2 = _integrationManager.AddApp(new FeedTarget(FeedTest.Test2Uri, feed2));
        _integrationManager.Invoking(x => x.AddAccessPoints(appEntry2, feed2, accessPoints2))
                           .Should().Throw<ConflictException>(because: "Should prevent access point conflicts");
        applyFlag2.Set.Should().BeFalse(because: "Should prevent access point conflicts");
    }

    [Fact]
    public void RemoveAccessPoints()
    {
        var capabilityList = CapabilityListTest.CreateTestCapabilityList();
        var testApp = new Feed {Name = "Test", CapabilityLists = {capabilityList}};

        using var unapplyFlag = new TemporaryFlagFile("0install-test-flag");
        var accessPoint = new MockAccessPoint {ID = "id1", Capability = "my_ext1", UnapplyFlagPath = unapplyFlag};

        // Inject access point into AppEntry (without running integration)
        var appEntry = _integrationManager.AddApp(new FeedTarget(FeedTest.Test1Uri, testApp));
        appEntry.AccessPoints = new() {Entries = {accessPoint}};

        _integrationManager.RemoveAccessPoints(appEntry, [accessPoint]);
        _integrationManager.AppList.Entries[0].AccessPoints!.Entries.Should().BeEmpty();

        unapplyFlag.Set.Should().BeTrue(because: "Unapply() should be called");

        _integrationManager.Invoking(x => x.RemoveAccessPoints(appEntry, [accessPoint]))
                           .Should().NotThrow(because: "Allow multiple removals of access points.");
    }

    [Fact]
    public void UpdateApp()
    {
        var capabilityList = new CapabilityList
        {
            OS = Architecture.CurrentSystem.OS,
            Entries = {new Model.Capabilities.FileType {ID = "my_ext1"}, new Model.Capabilities.FileType {ID = "my_ext2"}}
        };
        var feed = new Feed {Name = "Test 1", CapabilityLists = {capabilityList}};
        var accessPoints = new AccessPoint[] {new MockAccessPoint {ID = "id1", Capability = "my_ext1"}};

        var appEntry = _integrationManager.AddApp(new FeedTarget(FeedTest.Test1Uri, feed));
        _integrationManager.AddAccessPoints(appEntry, feed, accessPoints);
        _integrationManager.AppList.Entries[0].AccessPoints!.Entries
                           .Should().Equal(accessPoints, because: "All access points should be applied.");

        // Modify feed
        feed.Name = "Test 2";
        capabilityList.Entries.RemoveLast();

        _integrationManager.UpdateApp(appEntry, feed);
        appEntry.Name.Should().Be("Test 2");
        _integrationManager.AppList.Entries[0].AccessPoints!.Entries
                           .Should().Equal([accessPoints[0]], because: "Only the first access point should be left.");
    }

    [Fact]
    public void Repair()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, new Feed {Name = "Test"});
        var appEntry = _integrationManager.AddApp(target);

        using var applyFlag = new TemporaryFlagFile("0install-test-flag");
        // Inject access point into AppEntry (without running integration)
        appEntry.AccessPoints = new() {Entries = {new MockAccessPoint {ApplyFlagPath = applyFlag}}};
        _integrationManager.Repair(_ => new Feed {Name = "Test"});

        applyFlag.Set.Should().BeTrue(because: "Access points should be reapplied");
    }
}
