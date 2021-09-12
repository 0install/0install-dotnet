// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Solvers;

namespace ZeroInstall.Commands.Basic
{
    /// <summary>
    /// Check for updates to the program and download them if found.
    /// </summary>
    public class Update : Download
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public new const string Name = "update";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionUpdate;
        #endregion

        #region State
        private Selections? _oldSelections;

        /// <summary>
        /// Creates a new update command.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        public Update(ICommandHandler handler)
            : base(handler, outputOptions: false, refreshOptions: false)
        {}
        #endregion

        /// <inheritdoc/>
        public override ExitCode Execute()
        {
            if (SelectionsDocument) throw new NotSupportedException(Resources.NoSelectionsDocumentUpdate);

            try
            {
                OldSolve();
                Log.Info("Running Refresh Solve to find updates");
                RefreshSolve();
                Debug.Assert(UncachedImplementations != null);
            }
            catch (SolverException ex) when (Handler.Background)
            {
                Log.Info("Suppressed Solver-related error message due to background mode");
                Log.Info(ex);
                return ExitCode.SolverError;
            }

            Handle(UncachedImplementations);
            SelfUpdateCheck();

            return ShowOutput();
        }

        /// <summary>
        /// Run solver with refresh forced off to get the old values
        /// </summary>
        private void OldSolve()
        {
            FeedManager.Refresh = false;
            _oldSelections = Solver.Solve(Requirements);
        }

        /// <summary>
        /// Shows a list of changes found by the update process.
        /// </summary>
        protected override ExitCode ShowOutput()
        {
            Debug.Assert(_oldSelections != null && Selections != null);

            var diff = SelectionsManager.GetDiff(_oldSelections, Selections).ToList();

            if (diff.Count == 0)
            {
                Handler.OutputLow(Resources.NoUpdatesFound, Resources.NoUpdatesFound);
                return ExitCode.NoChanges;
            }
            else
            {
                Handler.Output(Resources.ChangesFound, diff);
                return ExitCode.OK;
            }
        }
    }
}
