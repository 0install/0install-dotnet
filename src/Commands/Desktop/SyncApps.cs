// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Desktop
{
    /// <summary>
    /// Synchronize the <see cref="AppList"/> with the server.
    /// </summary>
    public class SyncApps : IntegrationCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "sync";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionSync;

        /// <inheritdoc/>
        public override string Usage => "[OPTIONS]";

        /// <inheritdoc/>
        protected override int AdditionalArgsMax => 0;
        #endregion

        #region State
        private SyncResetMode _syncResetMode = SyncResetMode.None;

        /// <inheritdoc/>
        public SyncApps(ICommandHandler handler)
            : base(handler)
        {
            Options.Add("reset=", () => Resources.OptionSyncReset, (SyncResetMode mode) => _syncResetMode = mode);
        }
        #endregion

        /// <inheritdoc/>
        public override ExitCode Execute()
        {
            CheckInstallBase();

            try
            {
                using var syncManager = new SyncIntegrationManager(Config, FeedManager.GetFresh!, Handler, MachineWide);
                syncManager.Sync(_syncResetMode);
            }
            #region Error handling
            catch
            {
                // Suppress any left-over errors if the user canceled anyway
                Handler.CancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            #endregion

            finally
            {
                SelfUpdateCheck();
            }

            return ExitCode.OK;
        }
    }
}
