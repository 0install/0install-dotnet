// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Desktop
{
    /// <summary>
    /// Reintegrate all applications in the <see cref="AppList"/> into the desktop environment.
    /// </summary>
    public class RepairApps : IntegrationCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "repair-all";

        /// <summary>The alternative name of this command as used in command-line arguments in lower-case.</summary>
        public const string AltName = "repair-apps";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionRepairApps;

        /// <inheritdoc/>
        public override string Usage => "[OPTIONS]";

        /// <inheritdoc/>
        protected override int AdditionalArgsMax => 0;
        #endregion

        /// <inheritdoc/>
        public RepairApps(ICommandHandler handler)
            : base(handler)
        {}

        /// <inheritdoc/>
        public override ExitCode Execute()
        {
            CheckInstallBase();

            using var integrationManager = new IntegrationManager(Config, Handler, MachineWide);
            integrationManager.Repair(FeedManager.GetFresh!);

            return ExitCode.OK;
        }
    }
}
