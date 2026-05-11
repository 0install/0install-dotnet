// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Services.Solvers;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Add an application to the <see cref="AppList"/>.
/// </summary>
public class AddApp : AppCommand
{
    public const string Name = "add";
    public const string AltName = "add-app";
    public override string Description => Resources.DescriptionAddApp;
    public override string Usage => "[OPTIONS] [NAME] INTERFACE";
    protected override int AdditionalArgsMax => 2;

    private string? _command;
    private VersionRange? _version;

    /// <inheritdoc/>
    public AddApp(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("no-download", () => Resources.OptionNoDownload, _ => NoDownload = true);
        Options.Add("command=", () => Resources.OptionCommand, command => _command = command);
        Options.Add("version=", () => Resources.OptionVersionRange, (VersionRange range) => _version = range);
    }

    /// <summary>
    /// The window message ID (for use with <see cref="WindowsUtils.BroadcastMessage"/>) that signals that an application that is not listed in the <see cref="Catalog"/> was added.
    /// </summary>
    public static readonly int AddedNonCatalogAppWindowMessageID;

    static AddApp()
    {
        if (WindowsUtils.IsWindows)
            AddedNonCatalogAppWindowMessageID = WindowsUtils.RegisterWindowMessage("ZeroInstall.Commands.AddedNonCatalogApp");
    }

    /// <inheritdoc/>
    public override void Parse(IReadOnlyList<string> args)
    {
        base.Parse(args);

        if (_command != null && AdditionalArgs.Count < 2)
            throw new OptionException(string.Format(Resources.NoAddCommandWithoutAlias, "--command"), "command");
    }

    /// <inheritdoc/>
    protected override IEnumerable<string> BackgroundDownloadArgs
        => _version == null ? [] : ["--version", _version.ToString()];

    /// <inheritdoc/>
    protected override ExitCode ExecuteHelper()
    {
        try
        {
            AppEntry appEntry;
            if (AdditionalArgs is [var name, _])
            {
                PetName.Validate(name, nameof(name));

                var feed = FeedManager[InterfaceUri];
                var requirements = new Requirements {InterfaceUri = InterfaceUri, Command = _command};
                appEntry = IntegrationManager.AddApp(name, requirements, feed);
                CreateAlias(appEntry, name, _command);
            }
            else
            {
                appEntry = GetAppEntry(IntegrationManager, ref InterfaceUri);
            }

            if (_version != null) PinVersion(_version);

            var catalog = CatalogManager.TryGetCached() ?? new();
            if (WindowsUtils.IsWindows && !catalog.ContainsFeed(appEntry.EffectiveRequirements.InterfaceUri))
                WindowsUtils.BroadcastMessage(AddedNonCatalogAppWindowMessageID); // Notify Zero Install GUIs of changes

            return ExitCode.OK;
        }
        #region Error handling
        catch (InvalidOperationException ex)
            // WebException is a subclass of InvalidOperationException but we don't want to catch it here
            when (ex is not WebException)
        { // Application already in AppList
            Handler.OutputLow(Resources.DesktopIntegration, ex.Message);
            return ExitCode.NoChanges;
        }
        #endregion
    }

    /// <summary>
    /// Selects a specific version of the application and marks it as preferred for future runs.
    /// </summary>
    /// <exception cref="SolverException">The <see cref="ISolver"/> was unable to find an implementation matching <paramref name="versions"/>.</exception>
    private void PinVersion(VersionRange versions)
    {
        PinUtils.Unpin(InterfaceUri);
        SelectionCandidateProvider.Clear(); // Clear cache to pick up the preference changes

        PinUtils.Pin(
            Solver.Solve(new() {InterfaceUri = InterfaceUri, Versions = versions})
                  .MainImplementation);
    }
}
