// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NanoByte.Common.Collections;
using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.DesktopIntegration.ViewModel;
using ZeroInstall.Model;
using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Desktop
{
    /// <summary>
    /// Add an application to the <see cref="AppList"/> (if missing) and integrate it into the desktop environment.
    /// </summary>
    public sealed class IntegrateApp : AppCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "integrate";

        /// <summary>The alternative name of this command as used in command-line arguments in lower-case.</summary>
        public const string AltName = "integrate-app";

        /// <summary>Another alternative name of this command as used in command-line arguments in lower-case.</summary>
        public const string AltName2 = "desktop";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionIntegrateApp;

        /// <inheritdoc/>
        public override string Usage => "[OPTIONS] (ALIAS|INTERFACE)";
        #endregion

        #region State
        /// <summary>A list of all <see cref="AccessPoint"/> categories to be added to the already applied ones.</summary>
        private readonly List<string> _addCategories = new();

        /// <summary>A list of all <see cref="AccessPoint"/> categories to be removed from the already applied ones.</summary>
        private readonly List<string> _removeCategories = new();

        /// <inheritdoc/>
        public IntegrateApp(ICommandHandler handler)
            : base(handler)
        {
            Options.Add("no-download", () => Resources.OptionNoDownload, _ => NoDownload = true);

            Options.Add("add-standard", () => Resources.OptionIntegrateAddStandard, _ => _addCategories.AddRange(CategoryIntegrationManager.StandardCategories));
            Options.Add("add-all", () => Resources.OptionIntegrateAddAll, _ => _addCategories.AddRange(CategoryIntegrationManager.AllCategories));
            Options.Add("add=", () => Resources.OptionIntegrateAdd + Environment.NewLine + SupportedValues(CategoryIntegrationManager.AllCategories), category =>
            {
                category = category.ToLower();
                if (!CategoryIntegrationManager.AllCategories.Contains(category)) throw new OptionException(string.Format(Resources.InvalidArgument, category), "add");
                _addCategories.Add(category);
            });
            Options.Add("remove-all", () => Resources.OptionIntegrateRemoveAll, _ => _removeCategories.AddRange(CategoryIntegrationManager.AllCategories));
            Options.Add("remove=", () => Resources.OptionIntegrateRemove + Environment.NewLine + SupportedValues(CategoryIntegrationManager.AllCategories), category =>
            {
                category = category.ToLower();
                if (!CategoryIntegrationManager.AllCategories.Contains(category)) throw new OptionException(string.Format(Resources.InvalidArgument, category), "remove");
                _removeCategories.Add(category);
            });
        }
        #endregion

        /// <inheritdoc/>
        protected override ExitCode ExecuteHelper()
        {
            if (RemoveOnly)
            {
                IntegrationManager.RemoveAccessPointCategories(IntegrationManager.AppList[InterfaceUri], _removeCategories.ToArray());
                return ExitCode.OK;
            }
            else
            {
                CheckInstallBase();

                var appEntry = GetAppEntry(IntegrationManager, ref InterfaceUri);
                var feed = FeedManager[InterfaceUri];

                if (NoSpecifiedIntegrations)
                {
                    var state = new IntegrationState(IntegrationManager, appEntry, feed);
                    Retry:
                    Handler.ShowIntegrateApp(state);
                    try
                    {
                        state.ApplyChanges();
                    }
                    #region Error handling
                    catch (ConflictException ex)
                    {
                        if (Handler.Ask(
                            Resources.IntegrateAppInvalid + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine + Resources.IntegrateAppRetry,
                            defaultAnswer: false, alternateMessage: ex.Message))
                            goto Retry;
                    }
                    catch (InvalidDataException ex)
                    {
                        if (Handler.Ask(
                            Resources.IntegrateAppInvalid + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine + Resources.IntegrateAppRetry,
                            defaultAnswer: false, alternateMessage: ex.Message))
                            goto Retry;
                    }
                    #endregion

                    return ExitCode.OK;
                }
                else
                {
                    if (_removeCategories.Any())
                        IntegrationManager.RemoveAccessPointCategories(appEntry, _removeCategories.ToArray());
                    if (_addCategories.Any())
                        IntegrationManager.AddAccessPointCategories(appEntry, feed, _addCategories.ToArray());
                    return ExitCode.OK;
                }
            }
        }

        /// <summary>
        /// Determines whether the user specified only removals. This means we do not need to fetch any feeds.
        /// </summary>
        private bool RemoveOnly => !_addCategories.Any() && _removeCategories.Any();

        /// <summary>
        /// Determines whether the user specified no integration changes. This means we need a GUI to ask what to do.
        /// </summary>
        private bool NoSpecifiedIntegrations => !_addCategories.Any() && !_removeCategories.Any();

        /// <inheritdoc />
        protected override AppEntry GetAppEntry(IIntegrationManager integrationManager, ref FeedUri interfaceUri)
        {
            #region Sanity checks
            if (integrationManager == null) throw new ArgumentNullException(nameof(integrationManager));
            if (interfaceUri == null) throw new ArgumentNullException(nameof(interfaceUri));
            #endregion

            var appEntry = base.GetAppEntry(integrationManager, ref interfaceUri);
            var feed = FeedManager.GetFresh(interfaceUri);

            // Detect feed changes that may make an AppEntry update necessary
            if (!appEntry.CapabilityLists.UnsequencedEquals(feed.CapabilityLists))
            {
                string changedMessage = string.Format(Resources.CapabilitiesChanged, appEntry.Name);
                if (Handler.Ask(
                    changedMessage + " " + Resources.AskUpdateCapabilities,
                    defaultAnswer: false, alternateMessage: changedMessage))
                    integrationManager.UpdateApp(appEntry, feed);
            }

            return appEntry;
        }
    }
}
