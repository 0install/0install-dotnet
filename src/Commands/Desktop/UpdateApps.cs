// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Commands.Desktop
{
    /// <summary>
    /// Updates all applications in the <see cref="AppList"/>.
    /// </summary>
    public sealed class UpdateApps : IntegrationCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "update-all";

        /// <summary>The alternative name of this command as used in command-line arguments in lower-case.</summary>
        public const string AltName = "update-apps";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionUpdateApps;

        /// <inheritdoc/>
        public override string Usage => "[OPTIONS]";

        /// <inheritdoc/>
        protected override int AdditionalArgsMax => 0;
        #endregion

        #region State
        private bool _clean;

        /// <inheritdoc/>
        public UpdateApps(ICommandHandler handler)
            : base(handler)
        {
            Options.Add("c|clean", () => Resources.OptionClean, _ => _clean = true);
        }
        #endregion

        /// <inheritdoc/>
        public override ExitCode Execute()
        {
            var selectedImplementations = SolveAll(GetTargets()).ToList();
            DownloadUncachedImplementations(selectedImplementations);
            SelfUpdateCheck();

            if (_clean)
            {
                Handler.CancellationToken.ThrowIfCancellationRequested();
                Clean(selectedImplementations);
            }

            return ExitCode.OK;
        }

        /// <summary>
        /// Returns a list of <see cref="Requirements"/> that describe the applications to be updated.
        /// </summary>
        private IEnumerable<Requirements> GetTargets()
        {
            var appList = AppList.LoadSafe(MachineWide);

            // Target every application in the AppList...
            return from entry in appList.Entries
                   // ... unless excluded from auto-update
                   where entry.AutoUpdate
                   // ... or excluded by a hostname filter
                   where entry.Hostname == null || Regex.IsMatch(Environment.MachineName, entry.Hostname)
                   // Use custom app restrictions if any
                   select entry.Requirements ?? new Requirements(entry.InterfaceUri);
        }

        private IEnumerable<ImplementationSelection> SolveAll(IEnumerable<Requirements> targets)
        {
            FeedManager.Refresh = true;

            // Run solver for each app
            var implementations = new List<ImplementationSelection>();
            foreach (var requirements in targets)
            {
                Log.Info("Solving for " + requirements);
                implementations.AddRange(Solver.Solve(requirements).Implementations);
            }

            // Deduplicate selections
            return implementations.Distinct(ManifestDigestPartialEqualityComparer<ImplementationSelection>.Instance);
        }

        private void DownloadUncachedImplementations(IEnumerable<ImplementationSelection> selectedImplementations)
        {
            var selections = new Selections(selectedImplementations);
            var uncachedImplementations = SelectionsManager.GetUncachedSelections(selections).ToList();

            // Do not waste time on Fetcher subsystem if nothing is missing from cache
            if (uncachedImplementations.Count == 0) return;

            // Only show implementations in the UI that need to be downloaded
            Handler.ShowSelections(new Selections(uncachedImplementations), FeedManager);

            try
            {
                var toDownload = SelectionsManager.GetImplementations(uncachedImplementations);
                Fetcher.Fetch(toDownload);
            }
            #region Error handling
            catch
            {
                // Suppress any left-over errors if the user canceled anyway
                Handler.CancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            #endregion
        }

        private void Clean(IEnumerable<ImplementationSelection> implementations)
        {
            var digestsToKeep = implementations.Select(x => x.ManifestDigest);
            var digestsToRemove = ImplementationStore.ListAll().Except(digestsToKeep, ManifestDigestPartialEqualityComparer.Instance);
            Handler.RunTask(ForEachTask.Create(
                Resources.RemovingOutdated,
                digestsToRemove.ToList(),
                digest => ImplementationStore.Remove(digest, Handler)));
        }
    }
}
