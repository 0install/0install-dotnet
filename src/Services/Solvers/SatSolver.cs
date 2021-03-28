// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.SatSolver;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Uses a boolean satisfiability solver to solve <see cref="Requirements"/>.
/// </summary>
/// <param name="candidateProvider">Generates <see cref="SelectionCandidate"/>s for the solver to choose from.</param>
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
        private static readonly Solver<SelectionCandidate> _solver = new();

        protected override bool TryFulfill(SolverDemand demand)
        {
            foreach (var selection in demand.CandidatesCompatibleWith(Selections).ToSelections(demand))
            {
                if (_solver.IsSatisfiable(ToFormula(selection, demand.Requirements)))
                {
                    Selections.Implementations.Add(selection);
                    return true;
                }
            }

            return false;
        }

        private Formula<SelectionCandidate> ToFormula(ImplementationSelection selection, Requirements requirements)
        {
            var formula = new Formula<SelectionCandidate>();

            foreach (var subDemand in DemandsFor(selection, requirements).Where(x => x.Importance == Importance.Essential))
                formula.Add(Clauses.ExactlyOne(subDemand.Candidates.Select(Literal.Of)));
            return formula;
        }
    }
}
