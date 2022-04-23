// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text.RegularExpressions;
using NanoByte.Common.Native;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Updates all applications in the <see cref="AppList"/>.
/// </summary>
public class UpdateApps : IntegrationCommand
{
    public const string Name = "update-all";
    public const string AltName = "update-apps";
    public override string Description => Resources.DescriptionUpdateApps;
    public override string Usage => "[OPTIONS]";
    protected override int AdditionalArgsMax => 0;

    internal bool Clean;

    /// <inheritdoc/>
    public UpdateApps(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("c|clean", () => Resources.OptionClean, _ => Clean = true);
    }

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        if (Clean && WindowsUtils.IsWindows && !WindowsUtils.IsAdministrator && NonEmptyReadOnlyStores)
            throw new NotAdminException();

        var implementations = SolveAll(GetApps());
        DownloadUncachedImplementations(implementations);
        BackgroundSelfUpdate();

        if (Clean)
        {
            Handler.CancellationToken.ThrowIfCancellationRequested();
            CleanImplementations(implementations);
        }

        return ExitCode.OK;
    }

    private bool NonEmptyReadOnlyStores
        => ImplementationStore is CompositeImplementationStore composite
        && composite.Stores.Any(x => x.Kind == ImplementationStoreKind.ReadOnly && x.ListAll().Any());

    private IEnumerable<Requirements> GetApps()
        => AppList.LoadSafe(MachineWide).Entries
                  .Where(entry => entry.AutoUpdate && (entry.Hostname == null || Regex.IsMatch(Environment.MachineName, entry.Hostname)))
                  .Select(entry => entry.Requirements ?? entry.InterfaceUri);

    private ICollection<ImplementationSelection> SolveAll(IEnumerable<Requirements> apps)
    {
        FeedManager.Refresh = true;

        try
        {
            return AsParallel(apps)
                  .SelectMany(requirements => Solver.Solve(requirements).Implementations)
                  .Distinct(ManifestDigestPartialEqualityComparer<ImplementationSelection>.Instance)
                  .ToList();
        }
        #region Error handling
        catch (AggregateException ex)
        {
            ex.InnerExceptions.FirstOrDefault()?.Rethrow();
            throw;
        }
        #endregion
    }

    private void DownloadUncachedImplementations(IEnumerable<ImplementationSelection> implementations)
    {
        var uncachedImplementations = SelectionsManager.GetUncachedSelections(new Selections(implementations)).ToList();
        if (uncachedImplementations.Count == 0) return;

        Handler.ShowSelections(new Selections(uncachedImplementations), FeedManager);
        FetchAll(SelectionsManager.GetImplementations(uncachedImplementations));
    }

    private void CleanImplementations(IEnumerable<ImplementationSelection> implementations)
    {
        var digestsToKeep = implementations.Select(x => x.ManifestDigest);
        var digestsToRemove = ImplementationStore.ListAll().Except(digestsToKeep, ManifestDigestPartialEqualityComparer.Instance);
        Handler.RunTask(ForEachTask.Create(
            name: Resources.RemovingOutdated,
            target: digestsToRemove.ToList(),
            work: digest => ImplementationStore.Remove(digest, Handler)));
    }
}
