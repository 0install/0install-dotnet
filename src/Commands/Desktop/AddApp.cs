// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Net;
using JetBrains.Annotations;
using NanoByte.Common.Native;
using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Commands.Desktop
{
    /// <summary>
    /// Add an application to the <see cref="AppList"/>.
    /// </summary>
    public sealed class AddApp : AppCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public new const string Name = "add";

        /// <summary>The alternative name of this command as used in command-line arguments in lower-case.</summary>
        public const string AltName = "add-app";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionAddApp;

        /// <inheritdoc/>
        public override string Usage => "[OPTIONS] [ALIAS] INTERFACE";

        /// <inheritdoc/>
        protected override int AdditionalArgsMax => 2;
        #endregion

        #region State
        /// <inheritdoc/>
        public AddApp([NotNull] ICommandHandler handler)
            : base(handler)
        {
            Options.Add("no-download", () => Resources.OptionNoDownload, _ => NoDownload = true);
        }
        #endregion

        /// <summary>
        /// The window message ID (for use with <see cref="WindowsUtils.BroadcastMessage"/>) that signals that an application that is not listed in the <see cref="Catalog"/> was added.
        /// </summary>
        public static readonly int AddedNonCatalogAppWindowMessageID = WindowsUtils.RegisterWindowMessage("ZeroInstall.Commands.AddedNonCatalogApp");

        /// <inheritdoc/>
        protected override ExitCode ExecuteHelper()
        {
            try
            {
                var appEntry = GetAppEntry(IntegrationManager, ref InterfaceUri);

                if (AdditionalArgs.Count == 2)
                    CreateAlias(appEntry, AdditionalArgs[0]);

                if (!CatalogManager.GetCachedSafe().ContainsFeed(appEntry.InterfaceUri))
                    WindowsUtils.BroadcastMessage(AddedNonCatalogAppWindowMessageID); // Notify Zero Install GUIs of changes

                return ExitCode.OK;
            }
            #region Error handling
            catch (InvalidOperationException ex)
                // WebException is a subclass of InvalidOperationException but we don't want to catch it here
                when (!(ex is WebException))
            { // Application already in AppList
                Handler.OutputLow(Resources.DesktopIntegration, ex.Message);
                return ExitCode.NoChanges;
            }
            #endregion
        }
    }
}
