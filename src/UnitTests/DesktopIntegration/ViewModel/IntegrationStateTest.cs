// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Services;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.DesktopIntegration.ViewModel;

/// <summary>
/// Contains test methods for <see cref="IntegrationState"/>.
/// </summary>
[Collection("Desktop integration")]
public sealed class IntegrationStateTest : TestWithRedirect
{
    private readonly IntegrationManager _integrationManager;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public IntegrationStateTest()
    {
        _integrationManager = ExceptionUtils.Retry<UnauthorizedAccessException, IntegrationManager>( // Other processes might be competing for IntegrationManager mutex
            () => new(new Config(), new MockTaskHandler()));
    }

    public override void Dispose()
    {
        _integrationManager.Dispose();
        base.Dispose();
    }

    private (AppEntry appEntry, IntegrationState state) AddApp(FeedUri uri, string name, params AccessPoint[] accessPoints)
    {
        var feed = new Feed {Name = name};
        var appEntry = _integrationManager.AddApp(new FeedTarget(uri, feed));
        appEntry.AccessPoints = new() {Entries = {accessPoints}}; // Inject access points into AppEntry (without running integration)
        return (appEntry, new IntegrationState(_integrationManager, appEntry, feed));
    }

    [Fact]
    public void CheckForConflictsNone()
    {
        AddApp(FeedTest.Test1Uri, "App1", new AppAlias {Name = "alias1"});
        var (_, state) = AddApp(FeedTest.Test2Uri, "App2");
        state.Aliases.Add(new AppAlias {Name = "alias2"});

        state.CheckForConflicts();
    }

    [Fact]
    public void CheckForConflictsWithOtherApp()
    {
        AddApp(FeedTest.Test1Uri, "App1", new AppAlias {Name = "alias"});
        var (appEntry, state) = AddApp(FeedTest.Test2Uri, "App2");
        state.Aliases.Add(new AppAlias {Name = "alias"});

        state.Invoking(x => x.CheckForConflicts()).Should().Throw<ConflictException>();
        appEntry.AccessPoints!.Entries.Should().BeEmpty(because: "Checking must not apply anything");
    }

    [Fact]
    public void CheckForConflictsWithinSelection()
    {
        var (_, state) = AddApp(FeedTest.Test1Uri, "App1");
        state.Aliases.Add(new AppAlias {Name = "alias", Command = "command1"});
        state.Aliases.Add(new AppAlias {Name = "alias", Command = "command2"});

        state.Invoking(x => x.CheckForConflicts()).Should().Throw<ConflictException>();
    }

    [Fact]
    public void CheckForConflictsIgnoresOldVersionOfChangedAccessPoint()
    {
        var oldAlias = new AppAlias {Name = "alias", Command = "command1"};
        var (appEntry, state) = AddApp(FeedTest.Test1Uri, "App1", oldAlias);
        state.Aliases.Single().Command = "command2";

        state.CheckForConflicts();

        appEntry.AccessPoints!.Entries.Should().Equal([oldAlias], because: "Checking must not remove anything");
    }

    [Fact]
    public void CheckForConflictsAfterRemovingConflictingSelection()
    {
        AddApp(FeedTest.Test1Uri, "App1", new AppAlias {Name = "alias"});
        var (_, state) = AddApp(FeedTest.Test2Uri, "App2");
        var alias = new AppAlias {Name = "alias"};
        state.Aliases.Add(alias);
        state.Invoking(x => x.CheckForConflicts()).Should().Throw<ConflictException>();

        state.Aliases.Remove(alias);

        state.CheckForConflicts();
    }

    [Fact]
    public void CheckForConflictsSkipsAppForOtherHostname()
    {
        AddApp(FeedTest.Test1Uri, "App1", new AppAlias {Name = "alias"});
        var (appEntry, state) = AddApp(FeedTest.Test2Uri, "App2");
        appEntry.Hostname = "^no-such-host$";
        state.Aliases.Add(new AppAlias {Name = "alias"});

        state.CheckForConflicts();
    }

    [Fact]
    public void CheckForConflictsInvalidCapabilityReference()
    {
        AddApp(FeedTest.Test1Uri, "App1", new AccessPoints.FileType {Capability = "missing"});
        var (_, state) = AddApp(FeedTest.Test2Uri, "App2");

        state.Invoking(x => x.CheckForConflicts()).Should().Throw<InvalidDataException>();
    }
}
