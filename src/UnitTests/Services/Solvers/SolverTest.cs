// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;
using ZeroInstall.Model.Preferences;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Native;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Contains common code for testing specific <see cref="ISolver"/> implementations.
/// </summary>
public abstract class SolverTest : TestWithRedirect
{
    public static List<object[]> TestCases
    {
        get
        {
            using var stream = typeof(SolverTest).GetEmbeddedStream("test-cases.xml");
            return XmlStorage.LoadXml<TestCaseSet>(stream).TestCases.Select(x => new object[] {x}).ToList();
        }
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void TestCase(TestCase testCase)
    {
        if (testCase.Problem == null)
        {
            testCase.Selections?.Normalize();

            this.Invoking(x => x.Solve(testCase.Feeds, testCase.Requirements)
                                .Should().Be(testCase.Selections, testCase.ToString()))
                .Should().NotThrow(testCase.ToString());
        }
        else
        {
            this.Invoking(x => x.Solve(testCase.Feeds, testCase.Requirements))
                .Should().Throw<SolverException>(testCase.ToString()) /*.WithMessage(testCase.Problem)*/;
        }
    }

    [Fact]
    public void CustomFeedReference()
    {
        var interfaceUri = new FeedUri("http://example.com/prog1.xml");
        new InterfacePreferences
        {
            Uri = interfaceUri,
            Feeds = {new() {Source = new("http://example.com/prog2.xml")}}
        }.SaveFor(interfaceUri);

        var actual = Solve(
            feeds:
            [
                new Feed
                {
                    Uri = interfaceUri,
                    Name = "prog1",
                    Elements = {new Implementation {Version = new("1.0"), ID = "app1", Commands = {new() {Name = Command.NameRun, Path = "test-app1"}}}}
                },
                new Feed
                {
                    Uri = new("http://example.com/prog2.xml"),
                    Name = "prog2",
                    Elements = {new Implementation {Version = new("2.0"), ID = "app2", Commands = {new() {Name = Command.NameRun, Path = "test-app2"}}}}
                }
            ],
            requirements: new Requirements(interfaceUri, Command.NameRun));

        actual.Should().Be(new Selections
        {
            InterfaceUri = interfaceUri,
            Command = Command.NameRun,
            Implementations =
            {
                new()
                {
                    InterfaceUri = interfaceUri,
                    FromFeed = new("http://example.com/prog2.xml"),
                    Version = new("2.0"),
                    Stability = Stability.Testing,
                    ID = "app2",
                    Commands = {new() {Name = Command.NameRun, Path = "test-app2"}}
                }
            }
        });
    }

    [Fact]
    public void ExtraRestrictions()
    {
        var actual = Solve(
            feeds:
            [
                new Feed
                {
                    Uri = new("http://example.com/prog.xml"),
                    Name = "prog1",
                    Elements =
                    {
                        new Implementation {Version = new("1.0"), ID = "app1", Commands = {new() {Name = Command.NameRun, Path = "test-app1"}}},
                        new Implementation {Version = new("2.0"), ID = "app2", Commands = {new() {Name = Command.NameRun, Path = "test-app2"}}}
                    }
                }
            ],
            requirements: new Requirements(new("http://example.com/prog.xml"), Command.NameRun)
            {
                ExtraRestrictions = {{new("http://example.com/prog.xml"), new VersionRange("..!2.0")}}
            });

        actual.Should().Be(new Selections
        {
            InterfaceUri = new("http://example.com/prog.xml"),
            Command = Command.NameRun,
            Implementations =
            {
                new()
                {
                    InterfaceUri = new("http://example.com/prog.xml"),
                    Version = new("1.0"),
                    Stability = Stability.Testing,
                    ID = "app1",
                    Commands = {new() {Name = Command.NameRun, Path = "test-app1"}}
                }
            }
        });
    }

    private Selections Solve(IEnumerable<Feed> feeds, Requirements requirements)
    {
        var feedLookup = feeds.ToDictionary(
            keySelector: feed => feed.Uri,
            elementSelector: feed =>
            {
                feed.Normalize(feed.Uri);
                return feed;
            });

        var feedManagerMock = new Mock<IFeedManager>();
        feedManagerMock.Setup(x => x[It.IsAny<FeedUri>()]).Returns((FeedUri feedUri) =>
        {
            if (feedLookup.TryGetValue(feedUri, out var feed)) return feed;
            else throw new WebException($"Unable to fetch {feedUri}.");
        });
        feedManagerMock.Setup(x => x.GetPreferences(It.IsAny<FeedUri>()))
                       .Returns(new FeedPreferences());

        var candidateProvider = new SelectionCandidateProvider(new Config(), feedManagerMock.Object, Mock.Of<IImplementationStore>(), new CompositePackageManager([]));
        return BuildSolver(candidateProvider).Solve(requirements);
    }

    protected abstract ISolver BuildSolver(ISelectionCandidateProvider candidateProvider);
}
