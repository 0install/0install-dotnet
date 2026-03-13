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
/// Runs test methods for <see cref="BacktrackingSolver"/>.
/// </summary>
public class BacktrackingSolverTest : SolverTest
{
    protected override ISolver BuildSolver(ISelectionCandidateProvider candidateProvider)
        => new BacktrackingSolver(candidateProvider);

    [Fact]
    public void DiagnosticsForIncompatibleArchitecture()
    {
        var interfaceUri = new FeedUri("http://example.com/prog.xml");
        var feeds = new[]
        {
            new Feed
            {
                Uri = interfaceUri,
                Name = "prog",
                Elements =
                {
                    // Only provide an implementation for Linux-x86_64
                    new Implementation
                    {
                        Version = new("1.0"),
                        ID = "app1",
                        Architecture = new(OS.Linux, Cpu.X64),
                        Commands = {new() {Name = Command.NameRun, Path = "test-app1"}}
                    }
                }
            }
        };

        var requirements = new Requirements(interfaceUri, Command.NameRun)
        {
            // Request Windows-x86_64 which won't match
            Architecture = new(OS.Windows, Cpu.X64)
        };

        var exception = Assert.Throws<SolverException>(() => Solve(feeds, requirements));
        
        // Verify that the exception message contains diagnostic information
        exception.Message.Should().Contain("Diagnostics");
    }

    [Fact]
    public void DiagnosticsForVersionMismatch()
    {
        var interfaceUri = new FeedUri("http://example.com/prog.xml");
        var feeds = new[]
        {
            new Feed
            {
                Uri = interfaceUri,
                Name = "prog",
                Elements =
                {
                    new Implementation {Version = new("2.0"), ID = "app1", Commands = {new() {Name = Command.NameRun, Path = "test-app1"}}},
                    new Implementation {Version = new("3.0"), ID = "app2", Commands = {new() {Name = Command.NameRun, Path = "test-app2"}}}
                }
            }
        };

        // Request a version that doesn't exist (1.0)
        var requirements = new Requirements(interfaceUri, Command.NameRun)
        {
            ExtraRestrictions = {{interfaceUri, new VersionRange("1.0")}}
        };

        var exception = Assert.Throws<SolverException>(() => Solve(feeds, requirements));
        
        // Verify that the exception message contains diagnostic information
        exception.Message.Should().Contain("Diagnostics");
        exception.Message.Should().Contain("too old or too new");
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
}
