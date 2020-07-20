// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using Moq;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Model.Preferences;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Native;
using ZeroInstall.Store;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Contains common code for testing specific <see cref="ISolver"/> implementations.
    /// </summary>
    public abstract class SolverTest : TestWithRedirect
    {
        [Fact]
        public void TestCases()
        {
            static TestCaseSet Load()
            {
                using var stream = typeof(SolverTest).GetEmbeddedStream("test-cases.xml");
                return XmlStorage.LoadXml<TestCaseSet>(stream);
            }

            foreach (var testCase in Load().TestCases)
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
        }

        [Fact]
        public void CustomFeedReference()
        {
            new InterfacePreferences {Feeds = {new FeedReference {Source = new FeedUri("http://example.com/prog2.xml")}}}.SaveFor(new FeedUri("http://example.com/prog1.xml"));

            var actual = Solve(
                feeds: new[]
                {
                    new Feed
                    {
                        Uri = new FeedUri("http://example.com/prog1.xml"),
                        Elements = {new Implementation {Version = new ImplementationVersion("1.0"), ID = "app1", Commands = {new Command {Name = Command.NameRun, Path = "test-app1"}}}}
                    },
                    new Feed
                    {
                        Uri = new FeedUri("http://example.com/prog2.xml"),
                        Elements = {new Implementation {Version = new ImplementationVersion("2.0"), ID = "app2", Commands = {new Command {Name = Command.NameRun, Path = "test-app2"}}}}
                    }
                },
                requirements: new Requirements("http://example.com/prog1.xml", Command.NameRun));

            actual.Should().Be(new Selections
            {
                InterfaceUri = new FeedUri("http://example.com/prog1.xml"),
                Command = Command.NameRun,
                Implementations =
                {
                    new ImplementationSelection
                    {
                        InterfaceUri = new FeedUri("http://example.com/prog1.xml"),
                        FromFeed = new FeedUri("http://example.com/prog2.xml"),
                        Version = new ImplementationVersion("2.0"),
                        Stability = Stability.Testing,
                        ID = "app2",
                        Commands = {new Command {Name = Command.NameRun, Path = "test-app2"}}
                    }
                }
            });
        }

        [Fact]
        public void ExtraRestrictions()
        {
            var actual = Solve(
                feeds: new[]
                {
                    new Feed
                    {
                        Uri = new FeedUri("http://example.com/prog.xml"),
                        Elements =
                        {
                            new Implementation {Version = new ImplementationVersion("1.0"), ID = "app1", Commands = {new Command {Name = Command.NameRun, Path = "test-app1"}}},
                            new Implementation {Version = new ImplementationVersion("2.0"), ID = "app2", Commands = {new Command {Name = Command.NameRun, Path = "test-app2"}}}
                        }
                    }
                },
                requirements: new Requirements("http://example.com/prog.xml", Command.NameRun)
                {
                    ExtraRestrictions = {{new FeedUri("http://example.com/prog.xml"), new VersionRange("..!2.0")}}
                });

            actual.Should().Be(new Selections
            {
                InterfaceUri = new FeedUri("http://example.com/prog.xml"),
                Command = Command.NameRun,
                Implementations =
                {
                    new ImplementationSelection
                    {
                        InterfaceUri = new FeedUri("http://example.com/prog.xml"),
                        Version = new ImplementationVersion("1.0"),
                        Stability = Stability.Testing,
                        ID = "app1",
                        Commands = {new Command {Name = Command.NameRun, Path = "test-app1"}}
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

            var candidateProvider = new SelectionCandidateProvider(new Config(), feedManagerMock.Object, new Mock<IImplementationStore>(MockBehavior.Loose).Object, new StubPackageManager());
            return BuildSolver(candidateProvider).Solve(requirements);
        }

        protected abstract ISolver BuildSolver(ISelectionCandidateProvider candidateProvider);
    }
}
