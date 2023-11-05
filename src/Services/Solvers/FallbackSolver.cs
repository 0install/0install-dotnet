// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Wraps two solvers always passing requests to the primary one initially and falling back to secondary one should the primary one fail.
/// </summary>
/// <param name="primarySolver">The solver to run initially.</param>
/// <param name="secondarySolver">he solver to fall back to should <paramref name="primarySolver"/> fail.</param>
/// <remarks>This class is immutable and thread-safe.</remarks>
public sealed class FallbackSolver(ISolver primarySolver, ISolver secondarySolver) : ISolver
{
    /// <inheritdoc/>
    public Selections Solve(Requirements requirements)
    {
        Selections Handle(Exception ex)
        {
            Log.Info("Primary solver failed, falling back to secondary solver", ex);

            try
            {
                return secondarySolver.Solve(requirements);
            }
            catch (WebException ex2)
            {
                Log.Info("Unable to download secondary solver", ex2);
                throw ex.Rethrow(); // Report the original problem instead of inability to launch secondary solver
            }
        }

        try
        {
            return primarySolver.Solve(requirements);
        }
        catch (Exception ex) when (ex is SolverException or NotSupportedException)
        {
            return Handle(ex);
        }
    }
}
