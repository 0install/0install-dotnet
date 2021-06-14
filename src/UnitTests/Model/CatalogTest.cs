// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using FluentAssertions;
using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Contains test methods for <see cref="Catalog"/>.
    /// </summary>
    public class CatalogTest
    {
        /// <summary>
        /// Creates a fictive test <see cref="Catalog"/>.
        /// </summary>
        public static Catalog CreateTestCatalog() => new() {Feeds = {FeedTest.CreateTestFeed()}};

        /// <summary>
        /// Ensures that <see cref="Catalog.GetFeed"/> and <see cref="Catalog.this"/> correctly find contained <see cref="Feed"/>s.
        /// </summary>
        [Fact]
        public void GetFeed()
        {
            var catalog = CreateTestCatalog();

            catalog.GetFeed(FeedTest.Test1Uri).Should().Be(FeedTest.CreateTestFeed());
            catalog[FeedTest.Test1Uri].Should().Be(FeedTest.CreateTestFeed());

            catalog.GetFeed(new("http://invalid/")).Should().BeNull();
            Assert.Throws<KeyNotFoundException>(() => catalog[new FeedUri("http://invalid/")]);
        }

        /// <summary>
        /// Ensures that the class is correctly serialized and deserialized.
        /// </summary>
        [Fact]
        public void SaveLoad()
        {
            Catalog catalog1 = CreateTestCatalog(), catalog2;
            using (var tempFile = new TemporaryFile("0install-test-catalog"))
            {
                // Write and read file
                catalog1.SaveXml(tempFile);
                catalog2 = XmlStorage.LoadXml<Catalog>(tempFile);
            }

            // Ensure data stayed the same
            catalog2.Should().Be(catalog1, because: "Serialized objects should be equal.");
            catalog2.GetHashCode().Should().Be(catalog1.GetHashCode(), because: "Serialized objects' hashes should be equal.");
            catalog2.Should().NotBeSameAs(catalog1, because: "Serialized objects should not return the same reference.");
        }

        /// <summary>
        /// Ensures that the class can be correctly cloned.
        /// </summary>
        [Fact]
        public void Clone()
        {
            var catalog1 = CreateTestCatalog();
            var catalog2 = catalog1.Clone();

            // Ensure data stayed the same
            catalog2.Should().Be(catalog1, because: "Cloned objects should be equal.");
            catalog2.GetHashCode().Should().Be(catalog1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
            catalog2.Should().NotBeSameAs(catalog1, because: "Cloning should not return the same reference.");
        }

        /// <summary>
        /// Ensures that <see cref="Catalog.FindByShortName"/> works correctly.
        /// </summary>
        [Fact]
        public void FindByShortName()
        {
            var appA = new Feed
            {
                Uri = FeedTest.Test1Uri,
                Name = "AppA",
                EntryPoints = {new EntryPoint {Command = Command.NameRun, BinaryName = "BinaryA"}}
            };
            var appB = new Feed
            {
                Uri = FeedTest.Test2Uri,
                Name = "AppB",
                EntryPoints = {new EntryPoint {Command = Command.NameRun, BinaryName = "BinaryB"}}
            };
            var catalog = new Catalog {Feeds = {appA, appA.Clone(), appB, appB.Clone()}};

            catalog.FindByShortName("").Should().BeNull();
            catalog.FindByShortName("AppA").Should().BeSameAs(appA);
            catalog.FindByShortName("BinaryA").Should().BeSameAs(appA);
            catalog.FindByShortName("AppB").Should().BeSameAs(appB);
            catalog.FindByShortName("BinaryB").Should().BeSameAs(appB);
        }

        /// <summary>
        /// Ensures that <see cref="Catalog.Search"/> works correctly.
        /// </summary>
        [Fact]
        public void Search()
        {
            var appA = new Feed {Uri = FeedTest.Test1Uri, Name = "AppA"};
            var appB = new Feed {Uri = FeedTest.Test2Uri, Name = "AppB"};
            var lib = new Feed {Uri = FeedTest.Test3Uri, Name = "Lib"};
            var catalog = new Catalog {Feeds = {appA, appB, lib}};

            catalog.Search("").Should().Equal(appA, appB, lib);
            catalog.Search("App").Should().Equal(appA, appB);
            catalog.Search("AppA").Should().Equal(appA);
            catalog.Search("AppB").Should().Equal(appB);
        }
    }
}
