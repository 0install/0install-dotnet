// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration;
using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Reintegrate all applications in the <see cref="AppList"/> into the desktop environment.
/// </summary>
public class RepairApps : IntegrationCommand
{
    public const string Name = "repair-all";
    public const string AltName = "repair-apps";
    public override string Description => Resources.DescriptionRepairApps;
    public override string Usage => "[OPTIONS]";
    protected override int AdditionalArgsMax => 0;

    /// <inheritdoc/>
    public RepairApps(ICommandHandler handler)
        : base(handler)
    {}

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        CheckInstallBase();

        using var integrationManager = new IntegrationManager(Config, Handler, MachineWide);
        integrationManager.Repair(FeedManager.GetFresh);

        return ExitCode.OK;
    }
}
