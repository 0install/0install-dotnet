// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Preferences;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Native;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Runs test methods for <see cref="SelectionCandidateProvider"/>.
/// </summary>
public class SelectionCandidateProviderTest : TestWithMocksAndRedirect
{
    private readonly Mock<IFeedManager> _feedManagerMock;
    private readonly Mock<IPackageManager> _packageManagerMock;
    private readonly SelectionCandidateProvider _provider;

    public SelectionCandidateProviderTest()
    {
        _feedManagerMock = CreateMock<IFeedManager>();
        _feedManagerMock.Setup(x => x.GetPreferences(It.IsAny<FeedUri>()))
                        .Returns(new FeedPreferences());

        _packageManagerMock = CreateMock<IPackageManager>();

        _provider = new SelectionCandidateProvider(
            new Config(),
            _feedManagerMock.Object,
            new Mock<IImplementationStore>(MockBehavior.Loose).Object,
            _packageManagerMock.Object);
    }

    [Fact]
    public void GetSortedCandidates()
    {
        var mainFeed = FeedTest.CreateTestFeed();
        mainFeed.Feeds.Clear();
        _feedManagerMock.Setup(x => x[FeedTest.Test1Uri]).Returns(mainFeed);
        _packageManagerMock.Setup(x => x.Query((PackageImplementation)mainFeed.Elements[1])).Returns(Enumerable.Empty<ExternalImplementation>());

        var requirements = new Requirements(FeedTest.Test1Uri, Command.NameRun);
        _provider.GetSortedCandidates(requirements).Should().Equal(
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), (Implementation)mainFeed.Elements[0], requirements));
    }

    [Fact]
    public void FeedReferences()
    {
        var mainFeed = FeedTest.CreateTestFeed();
        _feedManagerMock.Setup(x => x[FeedTest.Test1Uri]).Returns(mainFeed);
        _packageManagerMock.Setup(x => x.Query((PackageImplementation)mainFeed.Elements[1])).Returns(Enumerable.Empty<ExternalImplementation>());

        var subFeed = mainFeed.Clone();
        subFeed.Uri = FeedTest.Sub1Uri;
        subFeed.Elements[0].Version = new("2.0");
        _feedManagerMock.Setup(x => x[FeedTest.Sub1Uri]).Returns(subFeed);

        var requirements = new Requirements(FeedTest.Test1Uri, Command.NameRun);
        _provider.GetSortedCandidates(requirements).Should().Equal(
            new SelectionCandidate(FeedTest.Sub1Uri, new FeedPreferences(), (Implementation)subFeed.Elements[0], requirements),
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), (Implementation)mainFeed.Elements[0], requirements));
    }

    [Fact]
    public void InterfacePreferences()
    {
        var mainFeed = FeedTest.CreateTestFeed();
        mainFeed.Elements.RemoveAt(1);
        mainFeed.Feeds.Clear();
        _feedManagerMock.Setup(x => x[FeedTest.Test1Uri]).Returns(mainFeed);

        new InterfacePreferences {Feeds = {new() {Source = FeedTest.Sub1Uri}}}.SaveFor(mainFeed.Uri);

        var subFeed = mainFeed.Clone();
        subFeed.Uri = FeedTest.Sub1Uri;
        subFeed.Elements[0].Version = new("2.0");
        _feedManagerMock.Setup(x => x[FeedTest.Sub1Uri]).Returns(subFeed);

        var requirements = new Requirements(FeedTest.Test1Uri, Command.NameRun);
        _provider.GetSortedCandidates(requirements).Should().Equal(
            new SelectionCandidate(FeedTest.Sub1Uri, new FeedPreferences(), (Implementation)subFeed.Elements[0], requirements),
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), (Implementation)mainFeed.Elements[0], requirements));
    }

    [Fact]
    public void NativeFeed()
    {
        var mainFeed = FeedTest.CreateTestFeed();
        mainFeed.Elements.RemoveAt(1);
        mainFeed.Feeds.Clear();
        _feedManagerMock.Setup(x => x[FeedTest.Test1Uri]).Returns(mainFeed);

        var localUri = new FeedUri(Locations.GetSaveDataPath("0install.net", true, "native_feeds", mainFeed.Uri.PrettyEscape()));

        var subFeed = mainFeed.Clone();
        subFeed.Uri = FeedTest.Sub1Uri;
        subFeed.Elements[0].Version = new("2.0");
        subFeed.SaveXml(localUri.LocalPath);
        _feedManagerMock.Setup(x => x[localUri]).Returns(subFeed);

        var requirements = new Requirements(FeedTest.Test1Uri, Command.NameRun);
        _provider.GetSortedCandidates(requirements).Should().Equal(
            new SelectionCandidate(localUri, new FeedPreferences(), (Implementation)subFeed.Elements[0], requirements),
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), (Implementation)mainFeed.Elements[0], requirements));
    }

    [Fact]
    public void SitePackages()
    {
        var mainFeed = FeedTest.CreateTestFeed();
        mainFeed.Feeds.Clear();
        _feedManagerMock.Setup(x => x[FeedTest.Test1Uri]).Returns(mainFeed);

        var pathComponents = mainFeed.Uri
                                     .EscapeComponent()
                                     .Prepend("site-packages")
                                     .Concat(new[] {"xyz", "0install", "feed.xml"});
        var localUri = new FeedUri(Locations.GetSaveDataPath("0install.net", isFile: true, resource: pathComponents.ToArray()));

        var subFeed = mainFeed.Clone();
        subFeed.Uri = FeedTest.Sub1Uri;
        subFeed.Elements[0].Version = new("2.0");
        subFeed.SaveXml(localUri.LocalPath);
        _feedManagerMock.Setup(x => x[localUri]).Returns(subFeed);
        _packageManagerMock.Setup(x => x.Query((PackageImplementation)mainFeed.Elements[1])).Returns(Enumerable.Empty<ExternalImplementation>());

        var requirements = new Requirements(FeedTest.Test1Uri, Command.NameRun);
        _provider.GetSortedCandidates(requirements).Should().Equal(
            new SelectionCandidate(localUri, new FeedPreferences(), (Implementation)subFeed.Elements[0], requirements),
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), (Implementation)mainFeed.Elements[0], requirements));
    }

    [Fact]
    public void PackageManager()
    {
        var mainFeed = FeedTest.CreateTestFeed();
        mainFeed.Feeds.Clear();
        _feedManagerMock.Setup(x => x[FeedTest.Test1Uri]).Returns(mainFeed);

        var nativeImplementation = new ExternalImplementation("rpm", "firefox", new("1.0")) {Languages = {"en-US"}};
        _packageManagerMock.Setup(x => x.Query((PackageImplementation)mainFeed.Elements[1])).Returns(new[] {nativeImplementation});

        var requirements = new Requirements(FeedTest.Test1Uri, Command.NameRun);

        _provider.GetSortedCandidates(requirements).Should().Equal(
            new SelectionCandidate(new(FeedUri.FromDistributionPrefix + FeedTest.Test1Uri), new FeedPreferences(), nativeImplementation, requirements),
            new SelectionCandidate(FeedTest.Test1Uri, new FeedPreferences(), (Implementation)mainFeed.Elements[0], requirements));
    }

    [Fact]
    public void LookupOriginalImplementation()
    {
        var mainFeed = FeedTest.CreateTestFeed();
        mainFeed.Feeds.Clear();
        _feedManagerMock.Setup(x => x[FeedTest.Test1Uri]).Returns(mainFeed);
        _packageManagerMock.Setup(x => x.Query((PackageImplementation)mainFeed.Elements[1])).Returns(Enumerable.Empty<ExternalImplementation>());

        var requirements = new Requirements(FeedTest.Test1Uri, Command.NameRun);
        var candidates = _provider.GetSortedCandidates(requirements);
        var candidate = candidates.Single().ToSelection(requirements, allCandidates: candidates);

        _provider.LookupOriginalImplementation(candidate)
                 .Should().Be(mainFeed.Elements[0]);
    }
}
