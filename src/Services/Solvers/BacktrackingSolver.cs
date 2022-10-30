// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Uses limited backtracking to solve <see cref="Requirements"/>. Does not find all possible solutions!
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
[PrimaryConstructor]
public partial class BacktrackingSolver : ISolver
{
    /// <summary>
    /// The maximum number backtracking steps to perform before giving up.
    /// </summary>
    private const int MaxBacktrackingSteps = 64;

    private readonly ISelectionCandidateProvider _candidateProvider;

    /// <inheritdoc/>
    public Selections Solve(Requirements requirements)
    {
        requirements = requirements.ForCurrentSystem();
        Log.Info($"Running Backtracking Solver for {requirements}");
        return new SolverRun(requirements, _candidateProvider).Solve();
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
