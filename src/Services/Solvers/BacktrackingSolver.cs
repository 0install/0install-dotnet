// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Uses limited backtracking to solve <see cref="Requirements"/>. Does not find all possible solutions!
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class BacktrackingSolver(ISelectionCandidateProvider candidateProvider) : ISolver
{
    /// <summary>
    /// The maximum number backtracking steps to perform before giving up.
    /// </summary>
    private const int MaxBacktrackingSteps = 64;

    /// <inheritdoc/>
    public Selections Solve(Requirements requirements)
    {
        requirements = requirements.ForCurrentSystem();
        Log.Info($"Running Backtracking Solver for {requirements}");
        return new SolverRun(requirements, candidateProvider).Solve();
    }

    private class SolverRun(Requirements requirements, ISelectionCandidateProvider candidateProvider) : SolverRunBase(requirements, candidateProvider)
    {
        private int _backtrackCounter;
        private readonly SolverDiagnostics _diagnostics = new();

        public new Selections Solve() => Solve(_diagnostics);

        protected override bool TryFulfill(SolverDemand demand)
        {
            var candidates = demand.CandidatesCompatibleWith(Selections);

            if (Selections.GetImplementation(demand.Requirements.InterfaceUri) is {} existingSelection)
            {
                // Ensure existing selection is one of the compatible candidates
                if (candidates.All(x => x.Implementation.ID != existingSelection.ID)) return false;

                if (!existingSelection.ContainsCommand(demand.Requirements.Command ?? Command.NameRun))
                { // Add additional command to selection if needed
                    var command = existingSelection.AddCommand(demand.Requirements, @from: CandidateProvider.LookupOriginalImplementation(existingSelection));
                    return command == null || TryFulfillAll(DemandsFor(command, demand.Requirements.InterfaceUri));
                }
                return true;
            }

            // Track unsuitable candidates for diagnostics
            foreach (var candidate in demand.Candidates.Where(c => !c.IsSuitable))
                _diagnostics.AddRejection(demand.Requirements.InterfaceUri, candidate, candidate.Notes ?? "Unsuitable");

            // Track incompatible candidates for diagnostics
            foreach (var candidate in demand.Candidates.Where(c => c.IsSuitable && !candidates.Contains(c)))
                _diagnostics.AddRejection(demand.Requirements.InterfaceUri, candidate, "Conflicts with existing selections");

            foreach (var selection in candidates.ToSelections(demand))
            {
                Selections.Implementations.Add(selection);
                if (TryFulfillAll(DemandsFor(selection, demand.Requirements))) return true;
                else
                {
                    _diagnostics.AddRejection(demand.Requirements.InterfaceUri, 
                        candidates.First(c => c.Implementation.ID == selection.ID), 
                        "Dependencies could not be satisfied");
                    Selections.Implementations.RemoveLast();
                }
            }
            
            if (demand.Importance == Importance.Recommended) return true; // Don't fail on unfulfillable non-essential dependencies, as long as they don't conflict with existing selections

            if (candidates.Any() == false)
                _diagnostics.AddNoCandidates(demand.Requirements.InterfaceUri, "No compatible candidates available");

            if (_backtrackCounter++ >= MaxBacktrackingSteps) throw new SolverException("Too much backtracking; dependency graph too complex.");
            return false;
        }

        private bool TryFulfillAll(IEnumerable<SolverDemand> demands)
        {
            var (essential, recommended) = demands.BucketizeImportance();

            // Quickly reject if there are impossible essential demands
            if (essential.Any(demand => !demand.CandidatesCompatibleWith(Selections).Any())) return false;

            var selectionsSnapshot = Selections.Clone(); // Create snapshot
            foreach (var essentialPermutation in essential.Permutate())
            {
                if (essentialPermutation.All(TryFulfill) && recommended.All(TryFulfill)) return true;
                else Selections = selectionsSnapshot.Clone(); // Restore snapshot when backtracking
            }
            return false;
        }
    }
}
