// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text.RegularExpressions;
using NanoByte.Common.Native;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Manages an <see cref="AppList"/> and desktop integration via <see cref="AccessPoint"/>s.
/// </summary>
/// <remarks>
/// To prevent race-conditions there may only be one desktop integration class instance active at any given time.
/// This class acquires a mutex upon calling its constructor and releases it upon calling <see cref="IDisposable.Dispose"/>.
/// </remarks>
[MustDisposeResource]
public class IntegrationManager : IntegrationManagerBase
{
    #region Constants
    /// <summary>
    /// The name of the cross-process mutex used to signal that a desktop integration process class is currently active.
    /// </summary>
    protected override string MutexName => "ZeroInstall.DesktopIntegration";

    /// <summary>
    /// The window message ID (for use with <see cref="WindowsUtils.BroadcastMessage"/>) that signals integration changes to interested observers.
    /// </summary>
    public static readonly int ChangedWindowMessageID;

    static IntegrationManager()
    {
        if (WindowsUtils.IsWindows)
            ChangedWindowMessageID = WindowsUtils.RegisterWindowMessage("ZeroInstall.DesktopIntegration");
    }
    #endregion

    /// <summary>
    /// User settings controlling network behaviour.
    /// </summary>
    protected readonly Config Config;

    /// <summary>
    /// Returns a path for a directory that can be used for desktop integration.
    /// </summary>
    /// <param name="machineWide"><c>true</c> if the directory should be machine-wide and machine-specific instead of roaming with the user profile.</param>
    /// <param name="resource">The directory name of the resource to be stored.</param>
    /// <returns>A fully qualified directory path. The directory is guaranteed to already exist.</returns>
    /// <exception cref="IOException">A problem occurred while creating a directory.</exception>
    /// <exception cref="UnauthorizedAccessException">Creating a directory is not permitted.</exception>
    /// <remarks>If a new directory is created with <paramref name="machineWide"/> set to <c>true</c> on Windows, ACLs are set to deny write access for non-Administrator users.</remarks>
    public static string GetDir(bool machineWide, params string[] resource)
        => machineWide
            ? Locations.GetSaveSystemConfigPath("0install.net", isFile: false, ["desktop-integration", ..resource])
            : Locations.GetSaveConfigPath("0install.net", isFile: false, ["desktop-integration", ..resource]);

    /// <summary>
    /// The storage location of the <see cref="AppList"/> file.
    /// </summary>
    protected readonly string AppListPath;

    #region Constructor
    /// <summary>
    /// Creates a new integration manager using the default <see cref="DesktopIntegration.AppList"/> (creating a new one if missing). Performs Mutex-based locking!
    /// </summary>
    /// <param name="config">User settings controlling network behaviour.</param>
    /// <param name="handler">A callback object used when the user is to be informed about the progress of long-running operations such as downloads.</param>
    /// <param name="machineWide">Apply operations machine-wide instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while accessing the <see cref="AppList"/> file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read or write access to the <see cref="AppList"/> file is not permitted or another desktop integration class is currently active.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing an XML file.</exception>
    public IntegrationManager(Config config, ITaskHandler handler, bool machineWide = false)
        : base(handler, machineWide)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));

        try
        {
            AcquireMutex();
        }
        catch (TimeoutException)
        {
            throw new UnauthorizedAccessException(Resources.IntegrationMutex);
        }

        try
        {
            AppListPath = AppList.GetDefaultPath(machineWide);
            if (File.Exists(AppListPath))
            {
                Log.Debug($"Loading AppList for IntegrationManager from: {AppListPath}");
                AppList = XmlStorage.LoadXml<AppList>(AppListPath);
            }
            else
            {
                Log.Debug($"Creating new AppList for IntegrationManager: {AppListPath}");
                AppList = new AppList();
                AppList.SaveXml(AppListPath);
            }
        }
        #region Error handling
        catch
        {
            // Avoid abandoned mutexes
            base.Dispose();
            throw;
        }
        #endregion
    }
    #endregion

    //--------------------//

    #region Apps
    /// <inheritdoc/>
    protected override AppEntry AddAppInternal(FeedTarget target)
    {
        // Prevent double entries
        if (AppList.ContainsEntry(target.Uri)) throw new InvalidOperationException(string.Format(Resources.AppAlreadyInList, target.Feed.Name));

        // Get basic metadata and copy of capabilities from feed
        var appEntry = new AppEntry
        {
            InterfaceUri = target.Uri,
            Name = target.Feed.Name,
            Timestamp = DateTime.UtcNow,
            CapabilityLists = {target.Feed.CapabilityLists.CloneElements()}
        };

        AppList.Entries.Add(appEntry);
        _appListChanged = true;

        WriteAppDir(appEntry);
        return appEntry;
    }

    /// <inheritdoc/>
    protected override AppEntry AddAppInternal(string petName, Requirements requirements, Feed feed)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(petName)) throw new ArgumentNullException(nameof(petName));
        if (requirements == null) throw new ArgumentNullException(nameof(requirements));
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        #endregion

        throw new NotImplementedException();
        /*
        // Prevent double entries
        if (AppList.ContainsEntry(petName)) throw new InvalidOperationException(string.Format(Resources.AppAlreadyInList, feed.Name));

        // Get basic metadata and copy of capabilities from feed
        var appEntry = new AppEntry {InterfaceUri = petName, Requirements = requirements, Name = feed.Name, Timestamp = DateTime.UtcNow};
        appEntry.CapabilityLists.Add(feed.CapabilityLists.CloneElements());

        AppList.Entries.Add(appEntry);
        _appListChanged = true;

        WriteAppDir(appEntry);
        return appEntry;
        */
    }

    /// <inheritdoc/>
    protected override void AddAppInternal(AppEntry prototype, Converter<FeedUri, Feed> feedRetriever)
    {
        #region Sanity checks
        if (prototype == null) throw new ArgumentNullException(nameof(prototype));
        if (feedRetriever == null) throw new ArgumentNullException(nameof(feedRetriever));
        #endregion

        var appEntry = prototype.Clone();
        AppList.Entries.Add(appEntry);
        _appListChanged = true;

        WriteAppDir(appEntry);

        if (appEntry.AccessPoints != null)
            AddAccessPointsInternal(appEntry, feedRetriever(appEntry.InterfaceUri), appEntry.AccessPoints.Clone().Entries);
    }

    /// <inheritdoc/>
    protected override void RemoveAppInternal(AppEntry appEntry)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        DeleteAppDir(appEntry);

        if (appEntry.AccessPoints != null)
        {
            // Unapply any remaining access points
            foreach (var accessPoint in appEntry.AccessPoints.Entries)
                accessPoint.Unapply(appEntry, MachineWide);
        }

        AppList.Entries.Remove(appEntry);
        _appListChanged = true;
    }

    /// <inheritdoc/>
    protected override void UpdateAppInternal(AppEntry appEntry, Feed feed)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        #endregion

        // Temporarily remove capability-based access points but remember them for later reapplication
        var toReapply = new List<AccessPoint>();
        if (appEntry.AccessPoints != null)
            toReapply.Add(appEntry.AccessPoints.Entries.Where(accessPoint => accessPoint is DefaultAccessPoint or CapabilityRegistration));
        RemoveAccessPointsInternal(appEntry, toReapply);

        // Update metadata and capabilities
        appEntry.Name = feed.Name;
        appEntry.CapabilityLists.Clear();
        appEntry.CapabilityLists.Add(feed.CapabilityLists.CloneElements());

        // Reapply removed access points dumping any that have become incompatible
        foreach (var accessPoint in toReapply)
        {
            try
            {
                AddAccessPointsInternal(appEntry, feed, [accessPoint]);
            }
            #region Error handling
            catch (KeyNotFoundException)
            {
                Log.Warn($"Access point '{accessPoint}' no longer compatible with interface '{appEntry.InterfaceUri}'.");
            }
            #endregion
        }

        WriteAppDir(appEntry);
        appEntry.Timestamp = DateTime.UtcNow;
    }
    #endregion

    #region AccessPoint
    /// <inheritdoc/>
    protected override void AddAccessPointsInternal(AppEntry appEntry, Feed feed, IReadOnlyCollection<AccessPoint> accessPoints)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        if (accessPoints == null) throw new ArgumentNullException(nameof(accessPoints));
        if (appEntry.AccessPoints != null && ReferenceEquals(appEntry.AccessPoints.Entries, accessPoints)) throw new ArgumentException("Must not be equal to appEntry.AccessPoints.Entries", nameof(accessPoints));
        #endregion

        // Skip entries with mismatching hostname
        if (appEntry.Hostname != null && !Regex.IsMatch(Environment.MachineName, appEntry.Hostname)) return;

        appEntry.AccessPoints ??= new();

        AppList.CheckForConflicts(accessPoints, appEntry);

        // Check if any access points are truly new, rather than just re-applying exist ones
        if (!accessPoints.All(appEntry.AccessPoints.Entries.Contains))
            _appListChanged = _integrationChanged = true;

        var iconStore = IconStores.DesktopIntegration(Config, Handler, MachineWide);

        // Load splash screen into icon store if specified, used by GUI for branding
        try
        {
            _ = feed.SplashScreens.GetIcon(Icon.MimeTypePng)
                   ?.To(iconStore.GetFresh);
        }
        #region Error handling
        catch (InvalidDataException ex)
        {
            Log.Warn(ex);
        }
        #endregion

        Handler.RunTask(ForEachTask.Create(string.Format(Resources.ApplyingIntegration, appEntry.Name),
            accessPoints,
            action: accessPoint => accessPoint.Apply(appEntry, feed, iconStore, MachineWide),
            rollback: accessPoint =>
            {
                // Don't perform rollback if the access point was already applied previously and this was only a refresh
                if (!appEntry.AccessPoints.Entries.Contains(accessPoint))
                    accessPoint.Unapply(appEntry, MachineWide);
            }));

        appEntry.AccessPoints.Entries.Remove(accessPoints); // Replace pre-existing entries
        appEntry.AccessPoints.Entries.Add(accessPoints);
        appEntry.Timestamp = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    protected override void RemoveAccessPointsInternal(AppEntry appEntry, IEnumerable<AccessPoint> accessPoints)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (accessPoints == null) throw new ArgumentNullException(nameof(accessPoints));
        #endregion

        if (appEntry.AccessPoints == null) return;

        accessPoints = accessPoints.ToList();
        foreach (var accessPoint in accessPoints)
        {
            accessPoint.Unapply(appEntry, MachineWide);
            _appListChanged = _integrationChanged = true;
        }

        // Remove the access points from the AppList
        appEntry.AccessPoints.Entries.Remove(accessPoints);
        appEntry.Timestamp = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    protected override void RepairAppInternal(AppEntry appEntry, Feed feed)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        #endregion

        var toReAdd = appEntry.AccessPoints?.Entries ?? [];
        if (toReAdd.Count != 0)
        {
            AddAccessPointsInternal(appEntry, feed, toReAdd.ToList());
            _integrationChanged = true;
        }

        WriteAppDir(appEntry);
    }
    #endregion

    #region Finish
    private bool _appListChanged;
    private bool _integrationChanged;

    /// <inheritdoc/>
    protected override void Finish()
    {
        Log.Debug($"Saving AppList to: {AppListPath}");
        // Retry to handle race conditions with read-only access to the file
        ExceptionUtils.Retry<IOException>(() => AppList.SaveXml(AppListPath));

        if (WindowsUtils.IsWindows && !Locations.IsPortable)
        {
            if (_appListChanged) WindowsUtils.BroadcastMessage(ChangedWindowMessageID);
            if (_integrationChanged) WindowsUtils.NotifyAssocChanged();
        }
        _appListChanged = _integrationChanged = false;
    }
    #endregion

    #region AppDir
    // ReSharper disable once UnusedParameter.Local
    private static void WriteAppDir(AppEntry appEntry)
    {
        // TODO: Implement
    }

    // ReSharper disable once UnusedParameter.Local
    private static void DeleteAppDir(AppEntry appEntry)
    {
        // TODO: Implement
    }
    #endregion
}
