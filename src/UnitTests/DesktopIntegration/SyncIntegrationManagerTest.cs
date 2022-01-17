// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using FluentAssertions;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Model;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Contains test methods for <see cref="SyncIntegrationManager"/>.
/// </summary>
[Collection("Desktop integration")]
public sealed class SyncIntegrationManagerTest : TestWithRedirect
{
    private const string CryptoKey = "abc123";

    #region Individual
    [Fact]
    public void AddedLocal()
    {
        using var apApplied = new TemporaryFlagFile("ap-applied");
        using var apNotApplied = new TemporaryFlagFile("ap-not-applied");
        var appListLocal = new AppList
        {
            Entries =
            {
                new AppEntry
                {
                    InterfaceUri = FeedTest.Test1Uri,
                    AccessPoints = new AccessPointList {Entries = {new MockAccessPoint {ApplyFlagPath = apApplied, UnapplyFlagPath = apNotApplied}}}
                }
            }
        };

        TestSync(SyncResetMode.None, appListLocal, new AppList(), new AppList());

        apApplied.Set.Should().BeFalse(because: "Locally existing access point should not be reapplied");
        apNotApplied.Set.Should().BeFalse(because: "Locally existing access point should not be removed");
    }

    [Fact]
    public void RemovedLocal()
    {
        using var apApplied = new TemporaryFlagFile("ap-applied");
        using var apNotApplied = new TemporaryFlagFile("ap-not-applied");
        var appListServer = new AppList
        {
            Entries =
            {
                new AppEntry
                {
                    InterfaceUri = FeedTest.Test1Uri,
                    AccessPoints = new AccessPointList {Entries = {new MockAccessPoint {ApplyFlagPath = apApplied, UnapplyFlagPath = apNotApplied}}}
                }
            }
        };

        TestSync(SyncResetMode.None, new AppList(), appListServer.Clone(), appListServer);

        apApplied.Set.Should().BeFalse(because: "Locally removed access point should not be reapplied");
        apNotApplied.Set.Should().BeFalse(because: "Locally removed access point should not be not-applied again");
    }

    [Fact]
    public void ModifiedLocal()
    {
        using var apLocalApplied = new TemporaryFlagFile("ap-local-applied");
        using var apLocalNotApplied = new TemporaryFlagFile("ap-local-not-applied");
        using var apRemoteApplied = new TemporaryFlagFile("ap-remote-applied");
        using var apRemoteNotApplied = new TemporaryFlagFile("ap-remote-not-applied");
        var appListLocal = new AppList
        {
            Entries =
            {
                new AppEntry
                {
                    InterfaceUri = FeedTest.Test1Uri,
                    AutoUpdate = true,
                    Timestamp = new DateTime(2001, 1, 1),
                    AccessPoints = new AccessPointList {Entries = {new MockAccessPoint {ApplyFlagPath = apLocalApplied, UnapplyFlagPath = apLocalNotApplied}}}
                }
            }
        };

        var appListServer = new AppList
        {
            Entries =
            {
                new AppEntry
                {
                    InterfaceUri = FeedTest.Test1Uri,
                    AutoUpdate = false,
                    Timestamp = new DateTime(2000, 1, 1),
                    AccessPoints = new AccessPointList {Entries = {new MockAccessPoint {ApplyFlagPath = apRemoteApplied, UnapplyFlagPath = apRemoteNotApplied}}}
                }
            }
        };

        TestSync(SyncResetMode.None, appListLocal, null, appListServer);

        apLocalApplied.Set.Should().BeFalse(because: "Up-to-date access point should not be reapplied");
        apLocalNotApplied.Set.Should().BeFalse(because: "Up-to-date access point should not be removed");
        apRemoteApplied.Set.Should().BeFalse(because: "Outdated access point should not be reapplied");
        apRemoteNotApplied.Set.Should().BeFalse(because: "Outdated access point should not be removed");
    }

    [Fact]
    public void AddedRemote()
    {
        using var apApplied = new TemporaryFlagFile("ap-applied");
        using var apNotApplied = new TemporaryFlagFile("ap-not-applied");
        var appListRemote = new AppList
        {
            Entries =
            {
                new AppEntry
                {
                    InterfaceUri = FeedTest.Test1Uri,
                    AccessPoints = new AccessPointList {Entries = {new MockAccessPoint {ApplyFlagPath = apApplied, UnapplyFlagPath = apNotApplied}, new MockAccessPoint()}}
                }
            }
        };

        TestSync(SyncResetMode.None, new AppList(), new AppList(), appListRemote);

        apApplied.Set.Should().BeTrue(because: "New access point should be applied");
        apNotApplied.Set.Should().BeFalse(because: "New access point should not be not-applied");
    }

    [Fact]
    public void RemovedRemote()
    {
        using var apApplied = new TemporaryFlagFile("ap-applied");
        using var apNotApplied = new TemporaryFlagFile("ap-not-applied");
        var appListLocal = new AppList
        {
            Entries =
            {
                new AppEntry
                {
                    InterfaceUri = FeedTest.Test1Uri,
                    AccessPoints = new AccessPointList {Entries = {new MockAccessPoint {ApplyFlagPath = apApplied, UnapplyFlagPath = apNotApplied}}}
                }
            }
        };

        TestSync(SyncResetMode.None, appListLocal, appListLocal.Clone(), new AppList());

        apApplied.Set.Should().BeFalse(because: "Removed access point should not be reapplied");
        apNotApplied.Set.Should().BeTrue(because: "Removed point should be not-applied");
    }

    [Fact]
    public void ModifiedRemote()
    {
        using var apLocalApplied = new TemporaryFlagFile("ap-local-applied");
        using var apLocalNotApplied = new TemporaryFlagFile("ap-local-not-applied");
        using var apRemoteApplied = new TemporaryFlagFile("ap-remote-applied");
        using var apRemoteNotApplied = new TemporaryFlagFile("ap-remote-not-applied");
        var appListLocal = new AppList
        {
            Entries =
            {
                new AppEntry
                {
                    InterfaceUri = FeedTest.Test1Uri,
                    AutoUpdate = true,
                    Timestamp = new DateTime(1999, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    AccessPoints = new AccessPointList {Entries = {new MockAccessPoint {ApplyFlagPath = apLocalApplied, UnapplyFlagPath = apLocalNotApplied}}}
                }
            }
        };

        var appListServer = new AppList
        {
            Entries =
            {
                new AppEntry
                {
                    InterfaceUri = FeedTest.Test1Uri,
                    AutoUpdate = false,
                    Timestamp = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    AccessPoints = new AccessPointList {Entries = {new MockAccessPoint {ApplyFlagPath = apRemoteApplied, UnapplyFlagPath = apRemoteNotApplied}}}
                }
            }
        };

        TestSync(SyncResetMode.None, appListLocal, null, appListServer);

        apLocalApplied.Set.Should().BeFalse(because: "Outdated access point should not be reapplied");
        apLocalNotApplied.Set.Should().BeTrue(because: "Outdated access point should be removed");
        apRemoteApplied.Set.Should().BeTrue(because: "New access point should be applied");
        apRemoteNotApplied.Set.Should().BeFalse(because: "New access point should not be not-applied");
    }

    /// <summary>
    /// Tests the sync logic with custom <see cref="AppList"/>s.
    /// </summary>
    /// <param name="resetMode">The <see cref="SyncResetMode"/> to pass to <see cref="SyncIntegrationManager.Sync"/>.</param>
    /// <param name="appListLocal">The current local <see cref="AppList"/>.</param>
    /// <param name="appListLast">The state of the <see cref="AppList"/> after the last successful sync.</param>
    /// <param name="appListServer">The current server-side <see cref="AppList"/>.</param>
    private static void TestSync(SyncResetMode resetMode, AppList appListLocal, AppList? appListLast, AppList appListServer)
    {
        string appListLocalPath = AppList.GetDefaultPath();
        appListLocal.SaveXml(appListLocalPath);
        appListLast?.SaveXml(appListLocalPath + SyncIntegrationManager.AppListLastSyncSuffix);

        using var appListServerPath = new TemporaryFile("0install-test-applist");
        {
            using (var stream = File.Create(appListServerPath))
                appListServer.SaveXmlZip(stream, CryptoKey);

            using (var appListServerFile = File.OpenRead(appListServerPath))
            {
                using var syncServer = new MicroServer("app-list", appListServerFile);
                var config = new Config
                {
                    SyncServer = new(syncServer.ServerUri),
                    SyncServerUsername = "dummy",
                    SyncServerPassword = "dummy",
                    SyncCryptoKey = CryptoKey
                };
                using (var integrationManager = new SyncIntegrationManager(config, _ => new Feed(), new SilentTaskHandler()))
                    integrationManager.Sync(resetMode);

                appListServer = AppList.LoadXmlZip(syncServer.FileContent, CryptoKey);
            }
        }

        appListLocal = XmlStorage.LoadXml<AppList>(appListLocalPath);
        appListLast = XmlStorage.LoadXml<AppList>(appListLocalPath + SyncIntegrationManager.AppListLastSyncSuffix);
        appListServer.Should().Be(appListLocal, because: "Server and local data should be equal after sync");
        appListLast.Should().Be(appListLocal, because: "Last sync snapshot and local data should be equal after sync");
    }
    #endregion

    #region Composite
    [Fact]
    public void Mixed()
    {
        using var ap1Applied = new TemporaryFlagFile("ap1-applied");
        using var ap1NotApplied = new TemporaryFlagFile("ap1-not-applied");
        using var ap2Applied = new TemporaryFlagFile("ap2-applied");
        using var ap2NotApplied = new TemporaryFlagFile("ap2-not-applied");
        using var ap3Applied = new TemporaryFlagFile("ap3-applied");
        using var ap3NotApplied = new TemporaryFlagFile("ap3-not-applied");
        using var ap4Applied = new TemporaryFlagFile("ap4-applied");
        using var ap4NotApplied = new TemporaryFlagFile("ap4-not-applied");
        TestSync(SyncResetMode.None, ap1Applied, ap1NotApplied, ap2Applied, ap2NotApplied, ap3Applied, ap3NotApplied, ap4Applied, ap4NotApplied);
        ap1Applied.Set.Should().BeFalse();
        ap1NotApplied.Set.Should().BeFalse();
        ap2Applied.Set.Should().BeFalse();
        ap2NotApplied.Set.Should().BeFalse();
        ap3Applied.Set.Should().BeTrue(because: "remote add: appEntry3");
        ap3NotApplied.Set.Should().BeFalse();
        ap4Applied.Set.Should().BeFalse();
        ap4NotApplied.Set.Should().BeTrue(because: "remote remove: appEntry4");
    }

    [Fact]
    public void ResetClient()
    {
        using var ap1Applied = new TemporaryFlagFile("ap1-applied");
        using var ap1NotApplied = new TemporaryFlagFile("ap1-not-applied");
        using var ap2Applied = new TemporaryFlagFile("ap2-applied");
        using var ap2NotApplied = new TemporaryFlagFile("ap2-not-applied");
        using var ap3Applied = new TemporaryFlagFile("ap3-applied");
        using var ap3NotApplied = new TemporaryFlagFile("ap3-not-applied");
        using var ap4Applied = new TemporaryFlagFile("ap4-applied");
        using var ap4NotApplied = new TemporaryFlagFile("ap4-not-applied");
        TestSync(SyncResetMode.Client, ap1Applied, ap1NotApplied, ap2Applied, ap2NotApplied, ap3Applied, ap3NotApplied, ap4Applied, ap4NotApplied);
        ap1Applied.Set.Should().BeFalse();
        ap1NotApplied.Set.Should().BeTrue(because: "undo: local add: appEntry1");
        ap2Applied.Set.Should().BeTrue(because: "undo: local remove: appEntry2");
        ap2NotApplied.Set.Should().BeFalse();
        ap3Applied.Set.Should().BeTrue(because: "remote add: appEntry3");
        ap3NotApplied.Set.Should().BeFalse();
        ap4Applied.Set.Should().BeFalse();
        ap4NotApplied.Set.Should().BeTrue(because: "remote remove: appEntry4");
    }

    [Fact]
    public void ResetServer()
    {
        using var ap1Applied = new TemporaryFlagFile("ap1-applied");
        using var ap1NotApplied = new TemporaryFlagFile("ap1-not-applied");
        using var ap2Applied = new TemporaryFlagFile("ap2-applied");
        using var ap2NotApplied = new TemporaryFlagFile("ap2-not-applied");
        using var ap3Applied = new TemporaryFlagFile("ap3-applied");
        using var ap3NotApplied = new TemporaryFlagFile("ap3-not-applied");
        using var ap4Applied = new TemporaryFlagFile("ap4-applied");
        using var ap4NotApplied = new TemporaryFlagFile("ap4-not-applied");
        TestSync(SyncResetMode.Server, ap1Applied, ap1NotApplied, ap2Applied, ap2NotApplied, ap3Applied, ap3NotApplied, ap4Applied, ap4NotApplied);
        ap1Applied.Set.Should().BeFalse();
        ap1NotApplied.Set.Should().BeFalse();
        ap2Applied.Set.Should().BeFalse();
        ap2NotApplied.Set.Should().BeFalse();
        ap3Applied.Set.Should().BeFalse();
        ap3NotApplied.Set.Should().BeFalse();
        ap4Applied.Set.Should().BeFalse();
        ap4NotApplied.Set.Should().BeFalse();
    }

    /// <summary>
    /// Tests the sync logic with pre-defined <see cref="AppList"/>s.
    /// local add: appEntry1, local remove: appEntry2, remote add: appEntry3, remote remove: appEntry4
    /// </summary>
    /// <param name="resetMode">The <see cref="SyncResetMode"/> to pass to <see cref="SyncIntegrationManager.Sync"/>.</param>
    /// <param name="ap1Applied">The flag file used to indicate that <see cref="MockAccessPoint.Apply"/> was called for appEntry1.</param>
    /// <param name="ap1NotApplied">The flag file used to indicate that <see cref="MockAccessPoint.Unapply"/> was called for appEntry1.</param>
    /// <param name="ap2Applied">The flag file used to indicate that <see cref="MockAccessPoint.Apply"/> was called for appEntry2.</param>
    /// <param name="ap2NotApplied">The flag file used to indicate that <see cref="MockAccessPoint.Unapply"/> was called for appEntry2.</param>
    /// <param name="ap3Applied">The flag file used to indicate that <see cref="MockAccessPoint.Apply"/> was called for appEntry3.</param>
    /// <param name="ap3NotApplied">The flag file used to indicate that <see cref="MockAccessPoint.Unapply"/> was called for appEntry3.</param>
    /// <param name="ap4Applied">The flag file used to indicate that <see cref="MockAccessPoint.Apply"/> was called for appEntry4.</param>
    /// <param name="ap4NotApplied">The flag file used to indicate that <see cref="MockAccessPoint.Unapply"/> was called for appEntry4.</param>
    private static void TestSync(SyncResetMode resetMode,
                                 TemporaryFlagFile ap1Applied,
                                 TemporaryFlagFile ap1NotApplied,
                                 TemporaryFlagFile ap2Applied,
                                 TemporaryFlagFile ap2NotApplied,
                                 TemporaryFlagFile ap3Applied,
                                 TemporaryFlagFile ap3NotApplied,
                                 TemporaryFlagFile ap4Applied,
                                 TemporaryFlagFile ap4NotApplied)
    {
        var appEntry1 = new AppEntry
        {
            InterfaceUri = FeedTest.Test1Uri,
            AccessPoints = new AccessPointList {Entries = {new MockAccessPoint {ApplyFlagPath = ap1Applied, UnapplyFlagPath = ap1NotApplied}}}
        };
        var appEntry2 = new AppEntry
        {
            InterfaceUri = FeedTest.Test2Uri,
            AccessPoints = new AccessPointList {Entries = {new MockAccessPoint {ApplyFlagPath = ap2Applied, UnapplyFlagPath = ap2NotApplied}}}
        };
        var appEntry3 = new AppEntry
        {
            InterfaceUri = FeedTest.Test3Uri,
            AccessPoints = new AccessPointList {Entries = {new MockAccessPoint {ApplyFlagPath = ap3Applied, UnapplyFlagPath = ap3NotApplied}}}
        };
        var appEntry4 = new AppEntry
        {
            InterfaceUri = new("http://example.com/test4.xml"),
            AccessPoints = new AccessPointList {Entries = {new MockAccessPoint {ApplyFlagPath = ap4Applied, UnapplyFlagPath = ap4NotApplied}}}
        };
        var appListLocal = new AppList {Entries = {appEntry1, appEntry4}};
        var appListLast = new AppList {Entries = {appEntry2, appEntry4}};
        var appListServer = new AppList {Entries = {appEntry2, appEntry3}};

        TestSync(resetMode, appListLocal, appListLast, appListServer);
    }
    #endregion
}
