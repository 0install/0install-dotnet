// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Commands.Desktop;
using ZeroInstall.Services;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// This behaves similarly to <see cref="Selection"/>, except that it also downloads the selected versions if they are not already cached.
/// </summary>
#if NETFRAMEWORK
[SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
#endif
public class Download : Selection
{
    public new const string Name = "download";
    public override string Description => Resources.DescriptionDownload;

    /// <summary>Indicates the user wants the implementation locations on the disk.</summary>
    private bool _show;

    /// <summary><see cref="Implementation"/>s referenced in <see cref="Selection.Selections"/> that are not available in the <see cref="IImplementationStore"/>.</summary>
    protected List<Implementation>? UncachedImplementations;

    /// <summary>
    /// Creates a new download command.
    /// </summary>
    /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
    public Download(ICommandHandler handler)
        : this(handler, outputOptions: true)
    {}

    /// <summary>
    /// Creates a new download command.
    /// </summary>
    /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
    /// <param name="outputOptions">Whether to add command-line options controlling output.</param>
    /// <param name="refreshOptions">Whether to add command-line options controlling refresh behavior.</param>
    /// <param name="customizeOptions">Whether to add command-line options for customizing selected implementations.</param>
    protected Download(ICommandHandler handler, bool outputOptions = true, bool refreshOptions = true, bool customizeOptions = true)
        : base(handler, outputOptions, refreshOptions, customizeOptions)
    {
        AddDiscoverOptions();

        if (outputOptions)
            Options.Add("show", () => Resources.OptionShow, _ => _show = true);
    }

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        Solve();
        if (FeedManager.ShouldRefresh)
        {
            Log.Info("Running Refresh Solve because feeds have become stale");
            RefreshSolve();
        }

        DownloadUncachedImplementations();
        BackgroundSelfUpdateAndClean();

        return ShowOutput();
    }

    /// <inheritdoc/>
    protected override NetworkLevel MinimumNetworkUseForBackgroundSelfUpdate
        => UncachedImplementations is {Count: > 0}
            ? NetworkLevel.Minimal // If we already downloaded other files already, we may as well do a self-update  too
            : NetworkLevel.Full;

    /// <summary>
    /// Automatically updates Zero Install itself in a background process.
    /// If no update check is due and we are in <see cref="ZeroInstallInstance.IsLibraryMode"/> instead removes outdated implementations in a background process.
    /// </summary>
    protected void BackgroundSelfUpdateAndClean()
    {
        if (BackgroundSelfUpdate()) return; // If a self-update check was started we shouldn't start any other cleanup tasks

        if (ZeroInstallInstance.IsLibraryMode // Only needed in library-mode because it lacks UI for manual clean
         && !ZeroInstallInstance.IsMachineWide // Machine-wide setups use scheduled task for clean instead
         && UncachedImplementations is {Count: 0} // Don't clean if we just downloaded new stuff (old versions may still be running)
         && !ImplementationsInReadOnlyStores // Avoid prompting for admin rights
         && !FeedManager.RateLimit(new("https://0install.net/background-clean-marker")))
        {
            Log.Info("Starting background removal of outdated implementations");
            StartCommandBackground(UpdateApps.Name, "--clean", "--batch");
        }
    }

    /// <inheritdoc/>
    [MemberNotNull(nameof(UncachedImplementations))]
    protected override void Solve()
    {
        base.Solve();

        try
        {
            UncachedImplementations = SelectionsManager.GetUncachedImplementations(Selections);
        }
        #region Error handling
        catch (KeyNotFoundException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new SolverException(ex.Message, ex);
        }
        #endregion
    }

    /// <summary>
    /// Downloads any <see cref="Implementation"/>s in <see cref="Selection"/> that are missing from <see cref="IImplementationStore"/>.
    /// </summary>
    /// <remarks>Makes sure <see cref="ISolver"/> ran with up-to-date feeds before downloading any implementations.</remarks>
    protected void DownloadUncachedImplementations()
    {
        if (UncachedImplementations is {Count: > 0} && !FeedManager.Refresh)
        {
            Log.Info("Running Refresh Solve because there are un-cached implementations");
            RefreshSolve();
        }

        if (CustomizeSelections || UncachedImplementations is {Count: > 0})
            ShowSelections();

        if (UncachedImplementations is {Count: > 0})
            FetchAll(UncachedImplementations);
    }

    protected override ExitCode ShowOutput()
    {
        if (_show || ShowXml) return base.ShowOutput();

        Handler.OutputLow(Resources.DownloadComplete, Resources.AllComponentsDownloaded);
        return ExitCode.OK;
    }
}
