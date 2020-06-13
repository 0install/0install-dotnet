// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using FluentAssertions;
using NanoByte.Common.Storage;
using Xunit;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Model;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Contains test methods for <see cref="AppList"/>.
    /// </summary>
    public sealed class AppListTest
    {
        #region Helpers
        /// <summary>
        /// Creates a fictive test <see cref="AppList"/> without <see cref="AccessPoint"/>s.
        /// </summary>
        private static AppList CreateTestAppListWithoutAPs() => new AppList
        {
            Entries =
            {
                new AppEntry
                {
                    InterfaceUri = FeedTest.Test1Uri,
                    AutoUpdate = true,
                    CapabilityLists = {Model.Capabilities.CapabilityListTest.CreateTestCapabilityList()}
                }
            }
        };

        /// <summary>
        /// Creates a fictive test <see cref="AppList"/> with <see cref="AccessPoint"/>s.
        /// </summary>
        private static AppList CreateTestAppListWithAPs() => new AppList
        {
            Entries =
            {
                new AppEntry
                {
                    InterfaceUri = FeedTest.Test1Uri,
                    AutoUpdate = true,
                    CapabilityLists = {Model.Capabilities.CapabilityListTest.CreateTestCapabilityList()},
                    AccessPoints = CreateTestAccessPointList()
                }
            }
        };

        /// <summary>
        /// Creates a fictive test <see cref="AccessPoints.AccessPointList"/>.
        /// </summary>
        private static AccessPointList CreateTestAccessPointList() => new AccessPointList
        {
            Entries =
            {
                new AppAlias {Command = "main", Name = "myapp"},
                new AutoStart {Command = "main", Name = "myapp"},
                new AutoPlay {Capability = "autoplay"},
                new CapabilityRegistration(),
                new ContextMenu {Capability = "context"},
                new DefaultProgram {Capability = "default"},
                new DesktopIcon {Command = "main", Name = "Desktop icon"},
                new FileType {Capability = "file_type"},
                new MenuEntry {Command = "main", Name = "Menu entry", Category = "Developer tools"},
                new SendTo {Command = "main", Name = "Send to"},
                new UrlProtocol {Capability = "protocol"},
                new QuickLaunch {Command = "main", Name = "Quick Launch"}
            }
        };
        #endregion

        [Fact] // Ensures that the class is correctly serialized and deserialized without AccessPoints.
        public void TestSaveLoadWithoutAPs() => TestSaveLoad(CreateTestAppListWithoutAPs());

        [Fact] // Ensures that the class is correctly serialized and deserialized with AccessPoints.
        public void TestSaveLoadWithAPs() => TestSaveLoad(CreateTestAppListWithAPs());

        private static void TestSaveLoad(AppList appList)
        {
            AppList appList2;
            using (var tempFile = new TemporaryFile("0install-unit-tests"))
            {
                // Write and read file
                appList.SaveXml(tempFile);
                appList2 = XmlStorage.LoadXml<AppList>(tempFile);
            }

            // Ensure data stayed the same
            appList2.Should().Be(appList, because: "Serialized objects should be equal.");
            appList2.GetHashCode().Should().Be(appList.GetHashCode(), because: "Serialized objects' hashes should be equal.");
            appList2.Should().NotBeSameAs(appList, because: "Serialized objects should not return the same reference.");
        }

        [Fact]
        public void TestContainsEntry()
        {
            var appList = CreateTestAppListWithAPs();
            appList.ContainsEntry(FeedTest.Test1Uri).Should().BeTrue();
            appList.ContainsEntry(FeedTest.Test2Uri).Should().BeFalse();
        }

        [Fact]
        public void TestGetEntry()
        {
            var appList = CreateTestAppListWithAPs();

            appList.GetEntry(FeedTest.Test1Uri).Should().Be(appList.Entries[0]);
            appList[FeedTest.Test1Uri].Should().Be(appList.Entries[0]);

            appList.GetEntry(FeedTest.Test2Uri).Should().BeNull();
            Assert.Throws<KeyNotFoundException>(() => appList[FeedTest.Test2Uri]);
        }

        [Fact]
        public void TestSearch()
        {
            var appA = new AppEntry {InterfaceUri = FeedTest.Test1Uri, Name = "AppA"};
            var appB = new AppEntry {InterfaceUri = FeedTest.Test2Uri, Name = "AppB"};
            var lib = new AppEntry {InterfaceUri = FeedTest.Test3Uri, Name = "Lib"};
            var appList = new AppList {Entries = {appA, appB, lib}};

            appList.Search("").Should().Equal(appA, appB, lib);
            appList.Search("App").Should().Equal(appA, appB);
            appList.Search("AppA").Should().Equal(appA);
            appList.Search("AppB").Should().Equal(appB);
        }

        [Fact] // Ensures that the class can be correctly cloned without AccessPoints.
        public void TestCloneWithoutAPs() => TestClone(CreateTestAppListWithoutAPs());

        [Fact] // Ensures that the class can be correctly cloned with AccessPoints.
        public void TestCloneWithAPs() => TestClone(CreateTestAppListWithAPs());

        private static void TestClone(AppList appList)
        {
            var appList2 = appList.Clone();

            // Ensure data stayed the same
            appList2.Should().Be(appList, because: "Cloned objects should be equal.");
            appList2.GetHashCode().Should().Be(appList.GetHashCode(), because: "Cloned objects' hashes should be equal.");
            appList2.Should().NotBeSameAs(appList, because: "Cloning should not return the same reference.");
        }

        [Fact]
        public void TestGetAppAlias()
        {
            var appAlias = new AppAlias {Name = "foobar"};
            var appEntry = new AppEntry {AccessPoints = new AccessPointList {Entries = {appAlias}}};
            var appList = new AppList {Entries = {appEntry}};

            appList.GetAppAlias("foobar", out var foundAppEntry)
                   .Should().Be(appAlias);
            foundAppEntry.Should().Be(appEntry);

            appList.GetAppAlias("other", out _).Should().BeNull();
        }

        [Fact]
        public void TestResolveAppAlias()
        {
            FeedUri uri = new FeedUri("http://example.com/test1.xml");
            var appList = new AppList
            {
                Entries =
                {
                    new AppEntry
                    {
                        AccessPoints = new AccessPointList {Entries =
                        {
                            new AppAlias {Name = "foobar", Command = Command.NameTest}
                        }},
                        InterfaceUri = uri
                    }
                }
            };

            appList.TryResolveAlias("foobar", out string? command).Should().Be(uri);
            command.Should().Be(Command.NameTest);

            appList.TryResolveAlias("other", out command).Should().BeNull();
            command.Should().BeNull();
        }
    }
}
