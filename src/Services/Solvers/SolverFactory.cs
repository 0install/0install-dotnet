// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Provides <see cref="ISolver"/> instances.
/// </summary>
public static class SolverFactory
{
    /// <summary>
    /// Creates a <see cref="ISolver"/>.
    /// </summary>
    /// <param name="config">User settings.</param>
    /// <param name="candidateProvider">Generates <see cref="SelectionCandidate"/>s for the solver to choose from.</param>
    public static ISolver Create(Config config, ISelectionCandidateProvider candidateProvider)
        => config.PreferSatSolver
            ? new SatSolver(candidateProvider)
            : new FallbackSolver(new BacktrackingSolver(candidateProvider), new SatSolver(candidateProvider));
}
