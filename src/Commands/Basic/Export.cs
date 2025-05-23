// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Commands.Basic.Exporters;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Exports all feeds and implementations required to launch the program specified by URI.
/// </summary>
#if NETFRAMEWORK
[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
[SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
#endif
public sealed class Export : Download
{
    public new const string Name = "export";
    public override string Description => Resources.DescriptionExport;
    public override string Usage => "[OPTIONS] URI DIRECTORY";
    protected override int AdditionalArgsMin => 2;
    protected override int AdditionalArgsMax => 2;

    private bool _noImplementations;
    private bool _includeZeroInstall;

    /// <summary>
    /// Creates a new export command.
    /// </summary>
    /// <param name="handler">A callback object used when the user needs to be asked questions or informed about download and IO tasks.</param>
    public Export(ICommandHandler handler)
        : base(handler, outputOptions: false)
    {
        Options.Add("no-implementations", () => Resources.OptionExportNoImplementations, _ => _noImplementations = true);
        Options.Add("include-zero-install", () => Resources.OptionExportIncludeZeroInstall, _ => _includeZeroInstall = true);

        Options.Add("bootstrap=", () => string.Format(Resources.DeprecatedOption, "0install run https://apps.0install.net/0install/0bootstrap.xml"), mode =>
        {
            if (!StringUtils.EqualsIgnoreCase(mode, "none"))
                throw new OptionException(string.Format(Resources.DeprecatedOption, "0install run https://apps.0install.net/0install/0bootstrap.xml"), "bootstrap");
        });
    }

    private string? _outputPath;

    /// <inheritdoc/>
    public override void Parse(IReadOnlyList<string> args)
    {
        base.Parse(args);

        try
        {
            _outputPath = Path.GetFullPath(AdditionalArgs[0]);
        }
        #region Error handling
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            // Wrap exception since only certain exception types are allowed
            throw new UriFormatException(ex.Message);
        }
        #endregion
    }

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        Solve();

        var exporter = new Exporter(Selections, Requirements.ForCurrentSystem().Architecture, _outputPath ?? throw new InvalidOperationException($"Must run {nameof(Parse)}() first."));

        exporter.ExportFeeds(FeedCache, OpenPgp);

        if (!_noImplementations)
        {
            DownloadUncachedImplementations();
            exporter.ExportImplementations(ImplementationStore, Handler);
        }

        if (FeedCache.GetFeed(Requirements.InterfaceUri) is {} feed)
        {
            exporter.ExportIcons(
                [..feed.Icons, ..feed.SplashScreens],
                IconStores.DesktopIntegration(Config, Handler, machineWide: false));
        }

        exporter.DeployImportScript();

        BackgroundSelfUpdate();

        return ShowOutput();
    }

    /// <inheritdoc/>
#pragma warning disable 8776
    [MemberNotNull(nameof(Selections))]
#pragma warning restore 8776
    protected override void Solve()
    {
        base.Solve();

        if (_includeZeroInstall)
        {
            try
            {
                var selfSelections = Solver.Solve(Config.SelfUpdateUri ?? new(Config.DefaultSelfUpdateUri));

                Selections.Implementations.Add(selfSelections.Implementations);
                UncachedImplementations.Add(SelectionsManager.GetUncachedImplementations(selfSelections));
            }
            #region Error handling
            catch
            {
                // Suppress any left-over errors if the user canceled anyway
                Handler.CancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            #endregion

            Handler.CancellationToken.ThrowIfCancellationRequested();
        }
    }

    protected override ExitCode ShowOutput()
    {
        Handler.OutputLow(
            title: Resources.ExportComplete,
            message: string.Format(Resources.AllComponentsExported, Selections?.Name, _outputPath));

        return ExitCode.OK;
    }
}
