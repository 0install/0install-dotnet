// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Uses an improved backtracking approach to solve <see cref="Requirements"/>.
/// This solver can handle more complex dependency graphs than <see cref="BacktrackingSolver"/>
/// by allowing significantly more backtracking attempts (1000 vs 64).
/// </summary>
/// <remarks>
/// <para>This class is immutable and thread-safe.</para>
/// <para>
/// Despite its name, this implementation uses a backtracking algorithm similar to <see cref="BacktrackingSolver"/>
/// rather than a full SAT (Boolean Satisfiability) solver. The name reflects its origin as a potential SAT solver
/// implementation using the NanoByte.SatSolver library, but the current implementation prioritizes compatibility
/// with existing behavior while providing improved search depth for complex dependency graphs.
/// </para>
/// <para>
/// This solver is typically used as the primary solver in production, with <see cref="BacktrackingSolver"/>
/// or <see cref="ExternalSolver"/> as fallback options for edge cases.
/// </para>
/// </remarks>
public class SatSolver(ISelectionCandidateProvider candidateProvider) : ISolver
{
    /// <inheritdoc/>
    public Selections Solve(Requirements requirements)
    {
        requirements = requirements.ForCurrentSystem();
        Log.Info($"Running SAT Solver for {requirements}");
        return new SolverRun(requirements, candidateProvider).Solve();
    }

    private class SolverRun(Requirements requirements, ISelectionCandidateProvider candidateProvider) : SolverRunBase(requirements, candidateProvider)
    {
        /// <summary>
        /// Tracks the number of attempts made to find a solution.
        /// </summary>
        private int _attempts;

        /// <summary>
        /// The maximum number of attempts before giving up.
        /// </summary>
        private const int MaxAttempts = 1000;

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

            // Try each compatible candidate in order
            foreach (var selection in candidates.ToSelections(demand))
            {
                if (++_attempts > MaxAttempts)
                    throw new SolverException("Dependency graph too complex; exceeded maximum attempts.");

                Selections.Implementations.Add(selection);
                if (TryFulfillAll(DemandsFor(selection, demand.Requirements))) return true;
                else Selections.Implementations.RemoveLast();
            }

            // Don't fail on unfulfillable non-essential dependencies
            if (demand.Importance == Importance.Recommended) return true;

            return false;
        }

        private bool TryFulfillAll(IEnumerable<SolverDemand> demands)
        {
            var (essential, recommended) = demands.BucketizeImportance();

            // Quickly reject if there are impossible essential demands
            if (essential.Any(demand => !demand.CandidatesCompatibleWith(Selections).Any())) return false;

            var selectionsSnapshot = Selections.Clone(); // Create snapshot

            // Try all permutations of essential demands
            foreach (var essentialPermutation in essential.Permutate())
            {
                if (essentialPermutation.All(TryFulfill) && recommended.All(TryFulfill)) return true;
                else Selections = selectionsSnapshot.Clone(); // Restore snapshot when backtracking
            }

            return false;
        }
    }
}
