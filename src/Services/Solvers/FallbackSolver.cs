// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Wraps two solvers always passing requests to the primary one initially and falling back to secondary one should the primary one fail.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
[PrimaryConstructor]
public sealed partial class FallbackSolver : ISolver
{
    /// <summary>
    /// The solver to run initially.
    /// </summary>
    private readonly ISolver _primarySolver;

    /// <summary>
    /// The solver to fall back to should <see cref="_primarySolver"/> fail.
    /// </summary>
    private readonly ISolver _secondarySolver;

    /// <inheritdoc/>
    public Selections Solve(Requirements requirements)
    {
        Selections Handle(Exception ex)
        {
            Log.Info("Primary solver failed, falling back to secondary solver.");
            Log.Debug(ex);

            try
            {
                return _secondarySolver.Solve(requirements);
            }
            catch (WebException ex2)
            {
                Log.Warn("Unable to download secondary solver");
                Log.Info(ex2);

                // Report the original problem instead of inability to launch secondary solver
                throw ex.Rethrow();
            }
        }

        try
        {
            return _primarySolver.Solve(requirements);
        }
        catch (SolverException ex)
        {
            return Handle(ex);
        }
        catch (NotSupportedException ex)
        {
            return Handle(ex);
        }
    }
}
