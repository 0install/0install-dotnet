// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Moq;
using NanoByte.Common.Storage;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Native;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Services
{
    /// <summary>
    /// Contains test methods for <see cref="SelectionsManager"/>.
    /// </summary>
    public class SelectionsManagerTest : TestWithMocks
    {
        private readonly Mock<IFeedManager> _feedManagerMock;
        private readonly Mock<IImplementationStore> _storeMock;
        private readonly Mock<IPackageManager> _packageManagerMock;
        private readonly ISelectionsManager _selectionsManager;

        public SelectionsManagerTest()
        {
            _feedManagerMock = CreateMock<IFeedManager>();
            _storeMock = CreateMock<IImplementationStore>();
            _packageManagerMock = CreateMock<IPackageManager>();
            _selectionsManager = new SelectionsManager(_feedManagerMock.Object, _storeMock.Object, _packageManagerMock.Object);
        }

        [Fact]
        public void GetUncachedSelections()
        {
            var selections = SelectionsTest.CreateTestSelections();

            _storeMock.Setup(x => x.Contains(selections.Implementations[0].ManifestDigest)).Returns(false);
            _storeMock.Setup(x => x.Contains(selections.Implementations[1].ManifestDigest)).Returns(true);

            var implementationSelections = _selectionsManager.GetUncachedSelections(selections);

            implementationSelections.Should().BeEquivalentTo(new[] {selections.Implementations[0]}, because: "Only the first implementation should be listed as uncached");
        }

        [Fact]
        public void GetUncachedSelectionsPackageManager()
        {
            using var tempFile = new TemporaryFile("0install-test-quicktest");
            var impl1 = new ExternalImplementation("RPM", "firefox", new("1.0")) {IsInstalled = false};
            var impl2 = new ExternalImplementation("RPM", "thunderbird", new("1.0")) {IsInstalled = true};
            var impl3 = new ExternalImplementation("RPM", "vlc", new("1.0")) {IsInstalled = true, QuickTestFile = tempFile};

            var selections = new Selections
            {
                InterfaceUri = FeedTest.Test1Uri,
                Command = Command.NameRun,
                Implementations =
                {
                    new ImplementationSelection {InterfaceUri = FeedTest.Test1Uri, FromFeed = new(FeedUri.FromDistributionPrefix + FeedTest.Test1Uri), ID = impl1.ID, QuickTestFile = impl1.QuickTestFile},
                    new ImplementationSelection {InterfaceUri = FeedTest.Test1Uri, FromFeed = new(FeedUri.FromDistributionPrefix + FeedTest.Test1Uri), ID = impl2.ID, QuickTestFile = impl2.QuickTestFile},
                    new ImplementationSelection {InterfaceUri = FeedTest.Test1Uri, FromFeed = new(FeedUri.FromDistributionPrefix + FeedTest.Test1Uri), ID = impl3.ID, QuickTestFile = impl3.QuickTestFile}
                }
            };

            _packageManagerMock.Setup(x => x.Lookup(selections.Implementations[0])).Returns(impl1);
            _packageManagerMock.Setup(x => x.Lookup(selections.Implementations[1])).Returns(impl2);

            var implementationSelections = _selectionsManager.GetUncachedSelections(selections);

            // Only the first implementation should be listed as uncached
            implementationSelections.Should().BeEquivalentTo(new[] {selections.Implementations[0]}, because: "Only the first implementation should be listed as uncached");
        }

        [Fact]
        public void GetImplementations()
        {
            var impl1 = new Implementation {ID = "test123"};
            var impl2 = new Implementation {ID = "test456"};
            var impl3 = new ExternalImplementation("RPM", "firefox", new("1.0"))
            {
                RetrievalMethods = {new ExternalRetrievalMethod {PackageID = "test"}}
            };
            var implementationSelections = new[]
            {
                new ImplementationSelection {ID = impl1.ID, InterfaceUri = FeedTest.Test1Uri},
                new ImplementationSelection {ID = impl2.ID, InterfaceUri = FeedTest.Test2Uri, FromFeed = FeedTest.Sub2Uri},
                new ImplementationSelection {ID = impl3.ID, InterfaceUri = FeedTest.Test1Uri, FromFeed = new(FeedUri.FromDistributionPrefix + FeedTest.Test1Uri)}
            };

            _feedManagerMock.Setup(x => x[FeedTest.Test1Uri]).Returns(new Feed {Elements = {impl1}});
            _feedManagerMock.Setup(x => x[FeedTest.Sub2Uri]).Returns(new Feed {Elements = {impl2}});
            _packageManagerMock.Setup(x => x.Lookup(implementationSelections[2])).Returns(impl3);

            _selectionsManager.GetImplementations(implementationSelections)
                              .Should().BeEquivalentTo(new[] {impl1, impl2, impl3});
        }

        [Fact]
        public void GetTree()
        {
            var digest1 = new ManifestDigest(sha256New: "a");
            var digest2 = new ManifestDigest(sha256New: "b");

            _storeMock.Setup(x => x.GetPath(digest1)).Returns("fake/path");
            _storeMock.Setup(x => x.GetPath(digest2)).Returns(() => null);

            var tree = _selectionsManager.GetTree(new Selections
            {
                InterfaceUri = new("http://root/"),
                Implementations =
                {
                    new ImplementationSelection
                    {
                        InterfaceUri = new("http://root/"),
                        ID = "a",
                        ManifestDigest = digest1,
                        Version = new("1.0"),
                        Dependencies =
                        {
                            new Dependency {InterfaceUri = new("http://dependency/")},
                            new Dependency {InterfaceUri = new("http://missing/")}
                        }
                    },
                    new ImplementationSelection
                    {
                        InterfaceUri = new("http://dependency/"),
                        ID = "b",
                        ManifestDigest = digest2,
                        Version = new("2.0"),
                        Dependencies = {new Dependency {InterfaceUri = new("http://root/")}} // Exercise cycle detection
                    }
                }
            });

            var node1 = new SelectionsTreeNode(new("http://root/"), new("1.0"), "fake/path", Parent: null);
            var node2 = new SelectionsTreeNode(new("http://dependency/"), new("2.0"), Path: null, Parent: node1);
            var node3 = new SelectionsTreeNode(new("http://missing/"), Version: null, Path: null, Parent: node1);
            tree.Should().Equal(node1, node2, node3);
        }

        [Fact]
        public void GetDiff() => _selectionsManager.GetDiff(
            oldSelections: new Selections
            {
                Implementations =
                {
                    new ImplementationSelection {InterfaceUri = new("http://feed1"), Version = new("1.0")},
                    new ImplementationSelection {InterfaceUri = new("http://feed2"), Version = new("1.0")}
                }
            }, newSelections: new Selections
            {
                Implementations =
                {
                    new ImplementationSelection {InterfaceUri = new("http://feed1"), Version = new("2.0")},
                    new ImplementationSelection {InterfaceUri = new("http://feed3"), Version = new("2.0")}
                }
            }).Should().BeEquivalentTo(
            new SelectionsDiffNode[]
            {
                new(new("http://feed1"), oldVersion: new("1.0"), newVersion: new("2.0")),
                new(new("http://feed2"), oldVersion: new("1.0")),
                new(new("http://feed3"), newVersion: new("2.0"))
            });
    }
}
