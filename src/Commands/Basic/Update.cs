// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Check for updates to the program and download them if found.
/// </summary>
public class Update : Download
{
    public new const string Name = "update";
    public override string Description => Resources.DescriptionUpdate;

    private Selections? _oldSelections;

    /// <summary>
    /// Creates a new update command.
    /// </summary>
    /// <param name="handler">A callback object used when the user needs to be asked questions or informed about download and IO tasks.</param>
    public Update(ICommandHandler handler)
        : base(handler, outputOptions: false, refreshOptions: false)
    {}

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        if (SelectionsDocument) throw new NotSupportedException(Resources.NoSelectionsDocumentUpdate);

        OldSolve();

        Log.Info("Running Refresh Solve to find updates");
        RefreshSolve();

        DownloadUncachedImplementations();
        BackgroundSelfUpdateAndClean();

        return ShowOutput();
    }

    /// <summary>
    /// Run solver with refresh forced off to get the old values.
    /// </summary>
    private void OldSolve()
    {
        using (PropertyPointer.For(() => FeedManager.Refresh).SetTemp(false))
            _oldSelections = Solver.Solve(Requirements);
    }

    /// <summary>
    /// Shows a list of changes found by the update process.
    /// </summary>
    protected override ExitCode ShowOutput()
    {
        Debug.Assert(_oldSelections != null && Selections != null);

        if (SelectionsManager.GetDiff(_oldSelections, Selections).ToList() is {Count: > 0} diff)
        {
            Handler.OutputLow(Resources.ChangesFound, diff);
            return ExitCode.OK;
        }

        if (UncachedImplementations is {Count: > 0})
        {
            Handler.OutputLow(Resources.DownloadComplete, Resources.AllComponentsDownloaded);
            return ExitCode.OK;
        }

        var selection = Selections.MainImplementation;
        if (selection.Candidates?.Max(x => x.Version) is {} maxVersion && maxVersion > selection.Version)
            Handler.OutputLow(Resources.NoUpdatesFound, string.Format(Resources.LaterVersionNotSelected, maxVersion, selection.Version));
        else
            Handler.OutputLow(Resources.NoUpdatesFound, Resources.NoUpdatesFound);
        return ExitCode.NoChanges;
    }
}
