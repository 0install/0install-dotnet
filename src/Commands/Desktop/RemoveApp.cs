// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.IO;
using NanoByte.Common;
using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration;

namespace ZeroInstall.Commands.Desktop
{
    /// <summary>
    /// Remove an application from the <see cref="AppList"/> and undoes any desktop environment integration.
    /// </summary>
    public class RemoveApp : AppCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "remove";

        /// <summary>The alternative name of this command as used in command-line arguments in lower-case.</summary>
        public const string AltName = "remove-app";

        /// <summary>Another alternative name of this command as used in command-line arguments in lower-case.</summary>
        public const string AltName2 = "destory";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionRemoveApp;

        /// <inheritdoc/>
        public override string Usage => "[OPTIONS] (ALIAS|INTERFACE)";
        #endregion

        /// <inheritdoc/>
        public RemoveApp(ICommandHandler handler)
            : base(handler)
        {}

        /// <inheritdoc/>
        protected override ExitCode ExecuteHelper()
        {
            try
            {
                IntegrationManager.RemoveApp(IntegrationManager.AppList[InterfaceUri]);
            }
            #region Sanity checks
            catch (KeyNotFoundException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion

            if (!ZeroInstallInstance.IsRunningFromCache && !ZeroInstallInstance.IsIntegrated && IntegrationManager.AppList.Entries.Count == 0)
            {
                Log.Info("Last app removed, auto-removing non-integrated Zero Install instance");
                StartCommandBackground(Self.Name, Self.Remove.Name, "--batch");
            }

            return ExitCode.OK;
        }
    }
}
