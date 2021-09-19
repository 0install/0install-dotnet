// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentAssertions;
using NanoByte.Common.Storage;
using Xunit;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Contains test methods for <see cref="Feed"/>.
    /// </summary>
    public class FeedTest
    {
        #region Helpers
        public static readonly FeedUri Test1Uri = new("http://example.com/test1.xml");
        public static readonly FeedUri Test2Uri = new("http://example.com/test2.xml");
        public static readonly FeedUri Test3Uri = new("http://example.com/test3.xml");
        public static readonly FeedUri Sub1Uri = new("http://example.com/sub1.xml");
        public static readonly FeedUri Sub2Uri = new("http://example.com/sub2.xml");
        public static readonly FeedUri Sub3Uri = new("http://example.com/sub3.xml");

        /// <summary>
        /// Creates a fictive test <see cref="Feed"/>.
        /// </summary>
        public static Feed CreateTestFeed() => new()
        {
            Uri = Test1Uri,
            Name = "MyApp",
            Categories = {"Category1", "Category2"},
            Homepage = new("http://example.com/"),
            Feeds = {new() {Source = Sub1Uri}},
            FeedFor = {new() {Target = new("http://example.com/super1.xml")}},
            Summaries = {"Default summary", {"de-DE", "German summary"}},
            Descriptions = {"Default description", {"de-DE", "German description"}},
            Icons = {new Icon {Href = new("http://example.com/test.png"), MimeType = Icon.MimeTypePng}},
            Elements = {CreateTestImplementation(), CreateTestPackageImplementation(), CreateTestGroup()},
            CapabilityLists = {CapabilityListTest.CreateTestCapabilityList()},
            EntryPoints =
            {
                new EntryPoint
                {
                    Command = Command.NameRun,
                    BinaryName = "myapp",
                    Names = {"Entry name", {"de-DE", "German entry name"}},
                    Summaries = {"Entry summary", {"de-DE", "German entry summary"}},
                    Icons = {new Icon {Href = new("http://example.com/test_command.png"), MimeType = Icon.MimeTypePng}}
                }
            }
        };

        /// <summary>
        /// Creates a fictive test <see cref="Implementation"/>.
        /// </summary>
        public static Implementation CreateTestImplementation() => new()
        {
            ID = "id1",
            ManifestDigest = new ManifestDigest(sha256: "123"),
            Version = new("1.0"),
            Architecture = new(OS.Windows, Cpu.I586),
            Languages = {"en-US"},
            Commands = {CommandTest.CreateTestCommand1()},
            DocDir = "doc",
            Stability = Stability.Developer,
            Dependencies =
            {
                new Dependency
                {
                    InterfaceUri = Test1Uri,
                    Constraints = {new Constraint {NotBefore = new("1.0"), Before = new("2.0")}},
                    Bindings = {EnvironmentBindingTest.CreateTestBinding(), OverlayBindingTest.CreateTestBinding(), ExecutableInVarTest.CreateTestBinding(), ExecutableInPathTest.CreateTestBinding()}
                }
            },
            Restrictions =
            {
                new Restriction
                {
                    InterfaceUri = Test2Uri,
                    Constraints = {new Constraint {Before = new("2.0")}}
                }
            },
            RetrievalMethods =
            {
                new Recipe
                {
                    Steps =
                    {
                        new Archive {Href = new("http://example.com/test.zip"), Size = 1024},
                        new SingleFile {Href = new("http://example.com/test.dat"), Size = 1024, Destination = "test.dat"},
                        new RenameStep {Source = "a", Destination = "b"},
                        new RemoveStep {Path = "c"}
                    }
                }
            }
        };

        /// <summary>
        /// Creates a fictive test <see cref="PackageImplementation"/>.
        /// </summary>
        public static PackageImplementation CreateTestPackageImplementation() => new()
        {
            Package = "firefox",
            Distributions = {"RPM"},
            Version = new("1.0"),
            Architecture = new(OS.Windows, Cpu.I586),
            Languages = {"en-US"},
            Commands = {CommandTest.CreateTestCommand1()},
            DocDir = "doc",
            Dependencies =
            {
                new Dependency
                {
                    InterfaceUri = Test2Uri,
                    Importance = Importance.Recommended,
                    Bindings = {EnvironmentBindingTest.CreateTestBinding(), OverlayBindingTest.CreateTestBinding(), ExecutableInVarTest.CreateTestBinding(), ExecutableInPathTest.CreateTestBinding()}
                }
            }
        };

        /// <summary>
        /// Creates a fictive test <see cref="Group"/>.
        /// </summary>
        private static Group CreateTestGroup() => new()
        {
            Languages = {"de"},
            Architecture = new(OS.FreeBsd, Cpu.I586),
            License = "GPL",
            Stability = Stability.Developer,
            Elements =
            {
                new Implementation
                {
                    ID = "a",
                    Version = new("1.0"),
                    Commands = {new Command {Name = "run", Path = "main1"}}
                },
                new Group
                {
                    Elements =
                    {
                        new Implementation
                        {
                            ID = "b",
                            Version = new("1.0"),
                            Commands = {new Command {Name = "run", Path = "main2"}}
                        }
                    }
                }
            }
        };
        #endregion

        /// <summary>
        /// Ensures that the class is correctly serialized and deserialized.
        /// </summary>
        [Fact]
        public void SaveLoad()
        {
            Feed feed1 = CreateTestFeed(), feed2;
            using (var tempFile = new TemporaryFile("0install-test-feed"))
            {
                // Write and read file
                feed1.SaveXml(tempFile);
                feed2 = XmlStorage.LoadXml<Feed>(tempFile);
            }

            // Ensure data stayed the same
            feed2.Should().Be(feed1, because: "Serialized objects should be equal.");
            feed2.GetHashCode().Should().Be(feed1.GetHashCode(), because: "Serialized objects' hashes should be equal.");
            feed2.Should().NotBeSameAs(feed1, because: "Serialized objects should not return the same reference.");
        }

        /// <summary>
        /// Ensures that the class can be correctly cloned and compared.
        /// </summary>
        [Fact]
        public void CloneEquals()
        {
            var feed1 = CreateTestFeed();
            feed1.Should().Be(feed1, because: "Equals() should be reflexive.");
            feed1.GetHashCode().Should().Be(feed1.GetHashCode(), because: "GetHashCode() should be reflexive.");

            var feed2 = feed1.Clone();
            feed2.Should().Be(feed1, because: "Cloned objects should be equal.");
            feed2.GetHashCode().Should().Be(feed1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
            feed2.Should().NotBeSameAs(feed1, because: "Cloning should not return the same reference.");

            feed2.Elements.Add(new Implementation {ID = "dummy"});
            feed2.Should().NotBe(feed1, because: "Modified objects should no longer be equal");
        }

        /// <summary>
        /// Ensures that <see cref="Feed.Normalize"/> correctly collapses <see cref="Group"/> structures.
        /// </summary>
        [Fact]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public void TestNormalizeGroup()
        {
            var feed = new Feed {Name = "Mock feed", Elements = {CreateTestGroup()}};
            feed.Normalize(Test1Uri);

            var implementation = feed.Elements[0];
            implementation.Architecture.Should().Be(new Architecture(OS.FreeBsd, Cpu.I586));
            implementation.Languages.ToString().Should().Be("de");
            implementation.License.Should().Be("GPL");
            implementation.Stability.Should().Be(Stability.Developer);
            implementation[Command.NameRun].Path.Should().Be("main1");

            implementation = feed.Elements[1];
            implementation.Architecture.Should().Be(new Architecture(OS.FreeBsd, Cpu.I586));
            implementation.Languages.ToString().Should().Be("de");
            implementation.License.Should().Be("GPL");
            implementation.Stability.Should().Be(Stability.Developer);
            implementation[Command.NameRun].Path.Should().Be("main2");
        }

        /// <summary>
        /// Ensures that <see cref="Feed.Normalize"/> correctly updates collection hash codes.
        /// </summary>
        [Fact]
        public void NormalizeHash()
        {
            var feed = CreateTestFeed();

            using var tempFile = new TemporaryFile("0install-test-feed");
            feed.SaveXml(tempFile);
            var feedReload = XmlStorage.LoadXml<Feed>(tempFile);

            feed.Normalize(new(tempFile));
            feedReload.Normalize(new(tempFile));
            feedReload.GetHashCode().Should().Be(feed.GetHashCode());
        }

        [Fact]
        public void NormalizeThrowsOnMissingName()
        {
            var feed = CreateTestFeed();
            feed.Name = null!;
            feed.Invoking(x => x.Normalize())
                .Should().Throw<InvalidDataException>().WithMessage("*<name>*");
        }

        [Fact]
        public void NormalizeThrowsOnMissingVersion()
        {
            var feed = CreateTestFeed();
            feed.Implementations.First().Version = null!;
            feed.Invoking(x => x.Normalize())
                .Should().Throw<InvalidDataException>().WithMessage("*version*<implementation*>*");
        }

        /// <summary>
        /// Ensures that <see cref="Feed.ResolveInternalReferences"/> correctly resolves <see cref="CopyFromStep.ID"/> references.
        /// </summary>
        [Fact]
        public void ResolveInternalReferences()
        {
            var step = new CopyFromStep {ID = "1"};
            var feed = new Feed
            {
                Name = "MyApp",
                Elements =
                {
                    new Implementation {ID = "1"},
                    new Implementation
                    {
                        ID = "2",
                        RetrievalMethods = {new Recipe {Steps = {step}}}
                    }
                }
            };

            feed.ResolveInternalReferences();

            step.Implementation.Should().Be(new Implementation {ID = "1"});
        }

        /// <summary>
        /// Ensures that <see cref="Feed.Strip"/> correctly removes non-essential metadata.
        /// </summary>
        [Fact]
        public void Strip()
        {
            var feed = CreateTestFeed();
            feed.Strip();

            feed.Elements.Should().BeEmpty();
            feed.CapabilityLists.Should().BeEmpty();
            feed.UnknownAttributes.Should().BeEmpty();
            feed.UnknownElements.Should().BeEmpty();
        }

        /// <summary>
        /// Ensures that contained <see cref="Implementation"/>s are correctly returned by ID.
        /// </summary>
        [Fact]
        public void GetImplementation()
        {
            var feed = CreateTestFeed();

            feed["id1"].Should().Be(CreateTestImplementation());
            Assert.Throws<KeyNotFoundException>(() => feed["invalid"]);
        }

        /// <summary>
        /// Ensures that <see cref="Feed.GetEntryPoint"/> correctly identifies contained <see cref="EntryPoint"/>s.
        /// </summary>
        [Fact]
        public void GetEntryPoint()
        {
            var feed = CreateTestFeed();

            feed.GetEntryPoint(null).Should().Be(CreateTestFeed().EntryPoints[0]);
            feed.GetEntryPoint("unknown").Should().BeNull();
        }

        /// <summary>
        /// Ensures that <see cref="Feed.GetBestName"/> correctly finds best matching names for <see cref="Command"/>s/<see cref="EntryPoint"/>s.
        /// </summary>
        [Fact]
        public void GetName()
        {
            var feed = CreateTestFeed();

            feed.GetBestName(CultureInfo.InvariantCulture, null).Should().Be("Entry name");
            feed.GetBestName(CultureInfo.InvariantCulture, "unknown").Should().Be(feed.Name + " unknown");
        }

        /// <summary>
        /// Ensures that <see cref="Feed.GetBestSummary"/> correctly finds best matching summaries for <see cref="Command"/>s/<see cref="EntryPoint"/>s.
        /// </summary>
        [Fact]
        public void GetSummary()
        {
            var feed = CreateTestFeed();

            feed.GetBestSummary(CultureInfo.InvariantCulture, null).Should().Be("Entry summary");
            feed.GetBestSummary(CultureInfo.InvariantCulture, "unknown").Should().Be("Default summary");
        }

        /// <summary>
        /// Ensures that <see cref="Feed.GetBestIcon"/> correctly finds best matching <see cref="Icon"/>s for <see cref="Command"/>s/<see cref="EntryPoint"/>s.
        /// </summary>
        [Fact]
        public void GetBestIcon()
        {
            var feed = CreateTestFeed();

            var feedIcon = new Icon {Href = new("http://example.com/test.png"), MimeType = Icon.MimeTypePng};
            var commandIcon = new Icon {Href = new("http://example.com/test_command.png"), MimeType = Icon.MimeTypePng};

            feed.GetBestIcon(Icon.MimeTypePng, null).Should().Be(commandIcon);
            feed.GetBestIcon(Icon.MimeTypePng, "unknown").Should().Be(feedIcon);
            feed.GetBestIcon("wrong", "unknown").Should().Be(null);
        }
    }
}
