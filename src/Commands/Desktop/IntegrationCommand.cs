// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Commands.Basic;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Common base class for commands that manage <see cref="DesktopIntegration"/>.
/// </summary>
public abstract class IntegrationCommand : CliCommand
{
    /// <summary>Do not download the application itself yet.</summary>
    protected bool NoDownload;

    /// <summary>Apply the operation machine-wide instead of just for the current user.</summary>
    internal bool MachineWide { get; set; }

    /// <inheritdoc/>
    protected IntegrationCommand(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("o|offline", () => Resources.OptionOffline, _ => Config.NetworkUse = NetworkLevel.Offline);
        Options.Add("r|refresh", () => Resources.OptionRefresh, _ => FeedManager.Refresh = true);

        Options.Add("m|machine", () => Resources.OptionMachine, _ => MachineWide = true);
    }

    /// <inheritdoc/>
    public override void Parse(IReadOnlyList<string> args)
    {
        base.Parse(args);

        if (MachineWide && WindowsUtils.IsWindows && !WindowsUtils.IsAdministrator)
            throw new NotAdminException(Resources.MustBeAdminForMachineWide);
    }

    /// <summary>
    /// Checks the current <see cref="Locations.InstallBase"/> to determine whether it is suitable for operations that persist it.
    /// </summary>
    /// <remarks>
    /// This should be called before performing any operations that persist <see cref="Locations.InstallBase"/> somewhere, e.g. in generated shortcuts or stubs.
    /// It is not required for operations that only remove things from the system.
    /// </remarks>
    /// <exception cref="UnsuitableInstallBaseException">The current Zero Install instance is installed in a location unsuitable for the desired operation.</exception>
    protected void CheckInstallBase()
    {
        if (Locations.IsPortable)
        {
            Log.Warn(Resources.NoIntegrationFromPortable);

            if (Handler.Verbosity != Verbosity.Batch)
            {
                string[] deployArgs = MachineWide
                    ? new[] { Self.AltName, Self.Deploy.Name, "--restart-central", "--machine" }
                    : new[] { Self.AltName, Self.Deploy.Name, "--restart-central" };
                ProgramUtils.Run(ProgramUtils.CliAssemblyName, deployArgs, Handler);
            }

            // NOTE: Portable instances remain decoupled from local instances, so we do not use UnsuitableInstallBaseException here, which would redirect commands to other instances.
            throw new OperationCanceledException();
        }

        if (!ZeroInstallInstance.IsDeployed) throw new UnsuitableInstallBaseException(Resources.NoIntegrationDeployRequired, MachineWide);
        if (MachineWide && !ZeroInstallInstance.IsMachineWide) throw new UnsuitableInstallBaseException(Resources.NoMachineWideIntegrationFromPerUser, MachineWide);
    }

    /// <summary>
    /// Finds an existing <see cref="AppEntry"/> or creates a new one for a specific interface URI.
    /// </summary>
    /// <param name="integrationManager">Manages desktop integration operations.</param>
    /// <param name="interfaceUri">The interface URI to create an <see cref="AppEntry"/> for. Will be updated if <see cref="Feed.ReplacedBy"/> is set and accepted by the user.</param>
    protected virtual AppEntry GetAppEntry(IIntegrationManager integrationManager, ref FeedUri interfaceUri)
    {
        #region Sanity checks
        if (integrationManager == null) throw new ArgumentNullException(nameof(integrationManager));
        if (interfaceUri == null) throw new ArgumentNullException(nameof(interfaceUri));
        #endregion

        var existingEntry = integrationManager.AppList.GetEntry(interfaceUri);

        var target = GetTarget(ref interfaceUri, out bool replaced);

        return replaced && existingEntry != null
            ? ReplaceAppEntry(integrationManager, existingEntry, target)
            : existingEntry ?? CreateAppEntry(integrationManager, target);
    }

    private FeedTarget GetTarget(ref FeedUri interfaceUri, out bool replaced)
    {
        var feed = FeedManager[interfaceUri];

        if (feed.ReplacedBy?.Target != null
         && Handler.Ask(string.Format(Resources.FeedReplacedAsk, feed.Name, feed.Uri, feed.ReplacedBy.Target), defaultAnswer: false, alternateMessage: Resources.FeedReplaced))
        {
            interfaceUri = feed.ReplacedBy.Target;
            feed = FeedManager.GetFresh(interfaceUri);
            replaced = true;
        }
        else replaced = false;

        return new FeedTarget(interfaceUri, feed);
    }

    private AppEntry ReplaceAppEntry(IIntegrationManager integrationManager, AppEntry entry, FeedTarget newTarget)
    {
        integrationManager.RemoveApp(entry);
        var newEntry = CreateAppEntry(integrationManager, newTarget);
        if (entry.AccessPoints != null)
            integrationManager.AddAccessPoints(newEntry, newTarget.Feed, entry.AccessPoints.Entries);
        return newEntry;
    }

    private AppEntry CreateAppEntry(IIntegrationManager integrationManager, FeedTarget target)
    {
        BackgroundDownload(target.Uri);

        try
        {
            Log.Info("Creating app entry for " + target.Uri.ToStringRfc());
            return integrationManager.AddApp(target);
        }
        catch (InvalidOperationException ex) when (ex.GetType() == typeof(InvalidOperationException))
        {
            Log.Warn("Attempting to handle race condition while creating app entry for " + target.Uri.ToStringRfc());
            return integrationManager.AppList[target.Uri];
        }
    }

    /// <summary>
    /// Pre-download application in a background process for later use.
    /// </summary>
    private void BackgroundDownload(FeedUri interfaceUri)
    {
        if (!NoDownload && Config.NetworkUse == NetworkLevel.Full)
        {
            Log.Info("Starting background download for later use");
            StartCommandBackground(Download.Name, "--batch", interfaceUri.ToStringRfc());
        }
    }

    /// <summary>
    /// Indicates whether any desktop integration for apps has been performed yet.
    /// </summary>
    public static bool ExistingDesktopIntegration(bool machineWide = false)
        => AppList.LoadSafe(machineWide).Entries.Any(x => x.AccessPoints != null);
}
