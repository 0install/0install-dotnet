// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.DesktopIntegration.ViewModel;
using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Add an application to the <see cref="AppList"/> (if missing) and integrate it into the desktop environment.
/// </summary>
public class IntegrateApp : AppCommand
{
    public const string Name = "integrate";
    public const string AltName = "integrate-app";
    public const string AltName2 = "desktop";
    public override string Description => Resources.DescriptionIntegrateApp;
    public override string Usage => "[OPTIONS] (ALIAS|INTERFACE)";

    /// <summary>A list of all <see cref="AccessPoint"/> categories to be added to the already applied ones.</summary>
    private readonly List<string> _addCategories = new();

    /// <summary>A list of all <see cref="AccessPoint"/> categories to be removed from the already applied ones.</summary>
    private readonly List<string> _removeCategories = new();

    /// <inheritdoc/>
    public IntegrateApp(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("no-download", () => Resources.OptionNoDownload, _ => NoDownload = true);

        string? NormalizeCategory(string category)
        {
            category = category.ToLower() switch
            {
                CapabilityRegistration.AltName => CapabilityRegistration.TagName,
                DefaultAccessPoint.AltName => DefaultAccessPoint.TagName,
                AppAlias.AltName => AppAlias.TagName,
                MenuEntry.AltName => MenuEntry.TagName,
                DesktopIcon.AltName => DesktopIcon.TagName,
                _ => category
            };
            return CategoryIntegrationManager.AllCategories.Contains(category) ? category : null;
        }

        Options.Add("add-standard", () => Resources.OptionIntegrateAddStandard, _ => _addCategories.Add(CategoryIntegrationManager.StandardCategories));
        Options.Add("add-all", () => Resources.OptionIntegrateAddAll, _ => _addCategories.Add(CategoryIntegrationManager.AllCategories));
        Options.Add("add=", () => Resources.OptionIntegrateAdd + Environment.NewLine + SupportedValues(CategoryIntegrationManager.AllCategories), category => _addCategories.Add(NormalizeCategory(category) ?? throw new OptionException(string.Format(Resources.InvalidArgument, category), "add")));
        Options.Add("remove-all", () => Resources.OptionIntegrateRemoveAll, _ => _removeCategories.Add(CategoryIntegrationManager.AllCategories));
        Options.Add("remove=", () => Resources.OptionIntegrateRemove + Environment.NewLine + SupportedValues(CategoryIntegrationManager.AllCategories), category => _removeCategories.Add(NormalizeCategory(category) ?? throw new OptionException(string.Format(Resources.InvalidArgument, category), "remove")));
    }

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
                            Resources.IntegrateAppInvalid + Environment.NewLine + ex.Message + Environment.NewLine + Resources.IntegrateAppRetry,
                            defaultAnswer: false, alternateMessage: ex.Message))
                        goto Retry;
                }
                catch (InvalidDataException ex)
                {
                    if (Handler.Ask(
                            Resources.IntegrateAppInvalid + Environment.NewLine + ex.GetMessageWithInner() + Environment.NewLine + Resources.IntegrateAppRetry,
                            defaultAnswer: false, alternateMessage: ex.GetMessageWithInner()))
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
