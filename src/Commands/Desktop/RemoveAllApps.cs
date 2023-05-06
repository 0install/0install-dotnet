// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Removes all applications from the <see cref="AppList"/> and undoes any desktop environment integration.
/// </summary>
public class RemoveAllApps : IntegrationCommand
{
    public const string Name = "remove-all";
    public const string AltName = "remove-all-apps";
    public override string Description => Resources.DescriptionRemoveAllApps;
    public override string Usage => "[OPTIONS]";
    protected override int AdditionalArgsMax => 0;

    /// <inheritdoc/>
    public RemoveAllApps(ICommandHandler handler)
        : base(handler)
    {}

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        if (!Handler.Ask(Resources.ConfirmRemoveAll, defaultAnswer: true))
            return ExitCode.NoChanges;

        using var integrationManager = new IntegrationManager(Config, Handler, MachineWide);
        Handler.RunTask(ForEachTask.Create(Resources.RemovingApplications,
            integrationManager.AppList.Entries.ToList(), integrationManager.RemoveApp));

        // Purge sync status, otherwise next sync would remove everything from server as well instead of restoring from there
        File.Delete(AppList.GetDefaultPath(MachineWide) + SyncIntegrationManager.AppListLastSyncSuffix);

        return ExitCode.OK;
    }
}
