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
                appEntry = AddNamedApp(name);
            else
            {
                appEntry = GetAppEntry(IntegrationManager, ref InterfaceUri);
                if (_version != null) SetVersion(appEntry, _version);
            }

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
    /// Adds a named app with its own <see cref="Requirements"/> and creates an alias with the same name for it.
    /// </summary>
    /// <param name="name">The pet-name for the app.</param>
    private AppEntry AddNamedApp(string name)
    {
        PetName.Validate(name, nameof(name));
        if (InterfaceUri.IsPetName) throw new UriFormatException(string.Format(Resources.NamedAppAsTarget, InterfaceUri.PetName));
        CheckInstallBase(); // Required for creating the alias, so check before modifying anything

        EnsureAllowed(InterfaceUri);
        var target = GetTarget(ref InterfaceUri, out _);

        var requirements = new Requirements {InterfaceUri = target.Uri, Command = _command};
        if (_version != null) requirements.Versions = _version;

        var appEntry = IntegrationManager.AddApp(name, requirements, target.Feed);
        try
        {
            CreateAlias(appEntry, name);
        }
        catch
        {
            // Do not leave behind an app entry without its alias
            IntegrationManager.RemoveApp(appEntry);
            throw;
        }

        BackgroundDownload(appEntry.InterfaceUri);
        return appEntry;
    }

    /// <summary>
    /// Restricts the versions of an existing app. Updates the <see cref="Requirements"/> of named apps and pins the version of the feed otherwise.
    /// </summary>
    private void SetVersion(AppEntry appEntry, VersionRange versions)
    {
        if (appEntry is {PetName: not null, Requirements: {} requirements})
        {
            requirements.Versions = versions;
            IntegrationManager.UpdateApp(appEntry, FeedManager[requirements.InterfaceUri], requirements);
        }
        else PinVersion(versions);
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
