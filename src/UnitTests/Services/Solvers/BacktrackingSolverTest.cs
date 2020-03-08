// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Tasks;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Runs test methods for <see cref="BacktrackingSolver"/>.
    /// </summary>
    public class BacktrackingSolverTest : SolverTest
    {
        protected override ISolver BuildSolver(ISelectionCandidateProvider candidateProvider)
            => new BacktrackingSolver(candidateProvider, new SilentTaskHandler());
    }
}
