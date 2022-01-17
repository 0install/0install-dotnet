// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ZeroInstall.Model;

namespace ZeroInstall.DesktopIntegration.ViewModel;

/// <summary>
/// A View-Model for modifying desktop integration. Provides data-binding lists and applies modifications in bulk.
/// </summary>
public partial class IntegrationState
{
    /// <summary>
    /// The integration manager used to apply selected integration options.
    /// </summary>
    private readonly IIntegrationManager _integrationManager;

    /// <summary>
    /// The application being integrated.
    /// </summary>
    public AppEntry AppEntry { get; }

    /// <summary>
    /// The feed providing additional metadata, icons, etc. for the application.
    /// </summary>
    public Feed Feed { get; }

    /// <summary>
    /// Creates a new integration state View-Model.
    /// </summary>
    /// <param name="integrationManager">The integration manager used to apply selected integration options.</param>
    /// <param name="appEntry">The application being integrated.</param>
    /// <param name="feed">The feed providing additional metadata, icons, etc. for the application.</param>
    public IntegrationState(IIntegrationManager integrationManager, AppEntry appEntry, Feed feed)
    {
        _integrationManager = integrationManager ?? throw new ArgumentNullException(nameof(integrationManager));
        AppEntry = appEntry ?? throw new ArgumentNullException(nameof(appEntry));
        Feed = feed ?? throw new ArgumentNullException(nameof(feed));

        CapabilityRegistration = (AppEntry.AccessPoints == null) || AppEntry.AccessPoints.Entries.OfType<AccessPoints.CapabilityRegistration>().Any();

        LoadCommandAccessPoints();
        LoadDefaultAccessPoints();
    }

    /// <summary>
    /// Applies any changes made to the View-Model to the underlying system.
    /// </summary>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="ConflictException">One or more of the new <see cref="AccessPoints.AccessPoint"/>s would cause a conflict with the existing <see cref="AccessPoints.AccessPoint"/>s in <see cref="IIntegrationManager.AppList"/>.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    public void ApplyChanges()
    {
        var toAdd = new List<AccessPoints.AccessPoint>();
        var toRemove = new List<AccessPoints.AccessPoint>();
        (CapabilityRegistration ? toAdd : toRemove).Add(new AccessPoints.CapabilityRegistration());
        CollectCommandAccessPointChanges(toAdd, toRemove);
        CollectDefaultAccessPointChanges(toAdd, toRemove);

        if (toRemove.Any()) _integrationManager.RemoveAccessPoints(AppEntry, toRemove);
        if (toAdd.Any()) _integrationManager.AddAccessPoints(AppEntry, Feed, toAdd);
    }
}
