// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Runs test methods for <see cref="SatSolver"/>.
/// </summary>
public class SatSolverTest : SolverTest
{
    protected override ISolver BuildSolver(ISelectionCandidateProvider candidateProvider)
        => new SatSolver(candidateProvider);
}
