// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Collections;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Uses limited backtracking to solve <see cref="Requirements"/>. Does not find all possible solutions!
    /// </summary>
    public class BacktrackingSolver : ISolver
    {
        /// <summary>
        /// The maximum number backtracking steps to perform before giving up.
        /// </summary>
        private const int MaxBacktrackingSteps = 64;

        private readonly ISelectionCandidateProvider _candidateProvider;

        /// <summary>
        /// Creates a new backtracking solver.
        /// </summary>
        /// <param name="candidateProvider">Generates <see cref="SelectionCandidate"/>s for the solver to choose from.</param>
        public BacktrackingSolver(ISelectionCandidateProvider candidateProvider)
        {
            _candidateProvider = candidateProvider ?? throw new ArgumentNullException(nameof(candidateProvider));
        }

        /// <inheritdoc/>
        public Selections Solve(Requirements requirements)
        {
            Log.Info($"Running Backtracking Solver for {requirements}");
            return new SolverRun(requirements.ForCurrentSystem(), _candidateProvider).Solve();
        }

        private class SolverRun : SolverRunBase
        {
            private int _backtrackCounter;

            public SolverRun(Requirements requirements, ISelectionCandidateProvider candidateProvider)
                : base(requirements, candidateProvider)
            {}

            protected override bool TryFulfill(SolverDemand demand, IEnumerable<SelectionCandidate> candidates)
            {
                foreach (var selection in candidates.ToSelections(demand))
                {
                    Selections.Implementations.Add(selection);
                    if (TryFulfill(DemandsFor(selection, demand.Requirements))) return true;
                    else Selections.Implementations.RemoveLast();
                }

                if (_backtrackCounter++ >= MaxBacktrackingSteps) throw new SolverException("Too much backtracking; dependency graph too complex.");
                return false;
            }

            protected override bool TryFulfill(IEnumerable<SolverDemand> demands)
            {
                var (essential, recommended) = Bucketize(demands);

                // Quickly reject impossible sets of demands
                if (essential.Any(demand => !demand.Candidates.Any(candidate => candidate.IsSuitable))) return false;

                var selectionsSnapshot = Selections.Clone(); // Create snapshot
                foreach (var essentialPermutation in essential.Permutate())
                {
                    if (essentialPermutation.All(TryFulfill))
                    {
                        recommended.ForEach(demand => TryFulfill(demand));
                        return true;
                    }
                    else Selections = selectionsSnapshot.Clone(); // Revert to snapshot
                }
                return false;
            }
        }
    }
}
