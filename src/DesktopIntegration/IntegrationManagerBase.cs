// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Base class for <see cref="IIntegrationManager"/> implementations using template methods.
/// </summary>
/// <param name="handler">A callback object used when the user is to be informed about the progress of long-running operations such as downloads.</param>
/// <param name="machineWide">Apply operations machine-wide instead of just for the current user.</param>
/// <exception cref="IOException">A problem occurred while accessing the <see cref="AppList"/> file.</exception>
/// <exception cref="UnauthorizedAccessException">Read or write access to the <see cref="AppList"/> file is not permitted or another desktop integration class is currently active.</exception>
/// <exception cref="InvalidDataException">A problem occurred while deserializing an XML file.</exception>
[MustDisposeResource]
public abstract class IntegrationManagerBase(ITaskHandler handler, bool machineWide = false) : ManagerBase(handler, machineWide), IIntegrationManager
{
    /// <summary>
    /// Stores a list of applications and their desktop integrations. Only use for read-access externally! Use this class' methods for any modifications.
    /// </summary>
    public AppList AppList { get; protected init; } = null!;

    #region Interface
    /// <inheritdoc/>
    public AppEntry AddApp(FeedTarget target)
    {
        var appEntry = AddAppInternal(target);
        Finish();
        return appEntry;
    }

    /// <inheritdoc/>
    public AppEntry AddApp(string petName, Requirements requirements, Feed feed)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(petName)) throw new ArgumentNullException(nameof(petName));
        if (requirements == null) throw new ArgumentNullException(nameof(requirements));
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        #endregion

        var appEntry = AddAppInternal(petName, requirements, feed);
        Finish();
        return appEntry;
    }

    /// <inheritdoc/>
    public void RemoveApp(AppEntry appEntry)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        try
        {
            RemoveAppInternal(appEntry);
        }
        catch (KeyNotFoundException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new InvalidDataException(ex.Message, ex);
        }
        finally
        {
            Finish();
        }
    }

    /// <inheritdoc/>
    public void UpdateApp(AppEntry appEntry, Feed feed)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        #endregion

        try
        {
            UpdateAppInternal(appEntry, feed);
        }
        catch (KeyNotFoundException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new InvalidDataException(ex.Message, ex);
        }
        finally
        {
            Finish();
        }
    }

    /// <inheritdoc/>
    public void UpdateApp(AppEntry appEntry, Feed feed, Requirements requirements)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        if (requirements == null) throw new ArgumentNullException(nameof(requirements));
        #endregion

        try
        {
            appEntry.Requirements = requirements;
            UpdateAppInternal(appEntry, feed);
        }
        catch (KeyNotFoundException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new InvalidDataException(ex.Message, ex);
        }
        finally
        {
            Finish();
        }
    }

    /// <inheritdoc/>
    public void AddAccessPoints(AppEntry appEntry, Feed feed, IEnumerable<AccessPoint> accessPoints)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        if (accessPoints == null) throw new ArgumentNullException(nameof(accessPoints));
        #endregion

        try
        {
            AddAccessPointsInternal(appEntry, feed, accessPoints.ToList());
        }
        catch (KeyNotFoundException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new InvalidDataException(ex.Message, ex);
        }
        finally
        {
            Finish();
        }
    }

    /// <inheritdoc/>
    public void RemoveAccessPoints(AppEntry appEntry, IEnumerable<AccessPoint> accessPoints)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        if (accessPoints == null) throw new ArgumentNullException(nameof(accessPoints));
        #endregion

        try
        {
            RemoveAccessPointsInternal(appEntry, accessPoints);
        }
        catch (KeyNotFoundException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new InvalidDataException(ex.Message, ex);
        }
        finally
        {
            Finish();
        }
    }

    /// <inheritdoc/>
    public void Repair(Converter<FeedUri, Feed> feedRetriever)
    {
        #region Sanity checks
        if (feedRetriever == null) throw new ArgumentNullException(nameof(feedRetriever));
        #endregion

        try
        {
            Handler.RunTask(ForEachTask.Create(Resources.RepairingIntegration,
                AppList.Entries, app => RepairAppInternal(app, feedRetriever(app.InterfaceUri))));
        }
        catch (KeyNotFoundException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new InvalidDataException(ex.Message, ex);
        }
        finally
        {
            Finish();
        }
    }
    #endregion

    #region Template methods
    /// <summary>
    /// Creates a new unnamed <see cref="AppEntry"/> and adds it to the <see cref="AppList"/>.
    /// </summary>
    /// <param name="target">The application to add.</param>
    /// <returns>The newly created application entry (already added to <see cref="AppList"/>).</returns>
    /// <exception cref="InvalidOperationException">The application is already in the list.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    protected abstract AppEntry AddAppInternal(FeedTarget target);

    /// <summary>
    /// Creates a new named <see cref="AppEntry"/> and adds it to the <see cref="AppList"/>.
    /// </summary>
    /// <param name="petName">The user-defined pet-name of the application.</param>
    /// <param name="requirements">The requirements describing the application to add.</param>
    /// <param name="feed">The feed providing additional metadata, capabilities, etc. for the application.</param>
    /// <returns>The newly created application entry (already added to <see cref="AppList"/>).</returns>
    /// <exception cref="InvalidOperationException">An application with the same <paramref name="petName"/> is already in the list.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    protected abstract AppEntry AddAppInternal(string petName, Requirements requirements, Feed feed);

    /// <summary>
    /// Creates a new <see cref="AppEntry"/> based on an existing prototype (applying any <see cref="AccessPoint"/>s) and adds it to the <see cref="AppList"/>.
    /// </summary>
    /// <param name="prototype">An existing <see cref="AppEntry"/> to use as a prototype.</param>
    /// <param name="feedRetriever">Callback method used to retrieve additional <see cref="Feed"/>s on demand.</param>
    protected abstract void AddAppInternal(AppEntry prototype, Converter<FeedUri, Feed> feedRetriever);

    /// <summary>
    /// Removes an <see cref="AppEntry"/> from the <see cref="AppList"/> while unapplying any remaining <see cref="AccessPoint"/>s.
    /// </summary>
    /// <param name="appEntry">The application to remove.</param>
    /// <exception cref="KeyNotFoundException">An <see cref="AccessPoint"/> reference to a <see cref="Capability"/> is invalid.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    protected abstract void RemoveAppInternal(AppEntry appEntry);

    /// <summary>
    /// Updates an <see cref="AppEntry"/> with new metadata and capabilities from a <see cref="Feed"/>. This may unapply and remove some existing <see cref="AccessPoint"/>s.
    /// </summary>
    /// <exception cref="KeyNotFoundException">An <see cref="AccessPoint"/> reference to a <see cref="Capability"/> is invalid.</exception>
    /// <param name="appEntry">The application entry to update.</param>
    /// <param name="feed">The feed providing additional metadata, capabilities, etc. for the application.</param>
    protected abstract void UpdateAppInternal(AppEntry appEntry, Feed feed);

    /// <summary>
    /// Applies <see cref="AccessPoint"/>s for an application.
    /// </summary>
    /// <param name="appEntry">The application being integrated.</param>
    /// <param name="feed">The feed providing additional metadata, icons, etc. for the application.</param>
    /// <param name="accessPoints">The access points to apply.</param>
    /// <exception cref="ArgumentException"><see cref="AccessPointList.Entries"/> from <paramref name="appEntry"/> is the same reference as <paramref name="accessPoints"/>.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="KeyNotFoundException">An <see cref="AccessPoint"/> reference to a <see cref="Capability"/> is invalid.</exception>
    /// <exception cref="ConflictException">One or more of the <paramref name="accessPoints"/> would cause a conflict with the existing <see cref="AccessPoint"/>s in <see cref="IIntegrationManager.AppList"/>.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    protected abstract void AddAccessPointsInternal(AppEntry appEntry, Feed feed, IReadOnlyCollection<AccessPoint> accessPoints);

    /// <summary>
    /// Removes already applied <see cref="AccessPoint"/>s for an application.
    /// </summary>
    /// <param name="appEntry">The <see cref="AppEntry"/> containing the <paramref name="accessPoints"/>.</param>
    /// <param name="accessPoints">The access points to unapply.</param>
    /// <exception cref="KeyNotFoundException">An <see cref="AccessPoint"/> reference to a <see cref="Capability"/> is invalid.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    protected abstract void RemoveAccessPointsInternal(AppEntry appEntry, IEnumerable<AccessPoint> accessPoints);

    /// <summary>
    /// Reapplies all <see cref="AccessPoint"/>s for a specific <see cref="AppEntry"/>.
    /// </summary>
    /// <param name="appEntry">The application entry to repair.</param>
    /// <param name="feed">The feed providing additional metadata, capabilities, etc. for the application.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="ConflictException"><paramref name="appEntry"/> conflicts with the rest of the <see cref="AppList"/>.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    protected abstract void RepairAppInternal(AppEntry appEntry, Feed feed);

    /// <summary>
    /// To be called after integration operations have been completed to inform the desktop environment and save the <see cref="DesktopIntegration.AppList"/>.
    /// </summary>
    protected abstract void Finish();
    #endregion
}
