// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Net;
using NanoByte.Common;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Wraps two solvers always passing requests to the primary one intially and falling back to secondary one should the primary one fail.
    /// </summary>
    public sealed class FallbackSolver : ISolver
    {
        /// <summary>
        /// The solver to run initially.
        /// </summary>
        private readonly ISolver _primarySolver;

        /// <summary>
        /// The solver to fall back to should <see cref="_primarySolver"/> fail.
        /// </summary>
        private readonly ISolver _secondarySolver;

        /// <summary>
        /// Creates a new fallback solver.
        /// </summary>
        /// <param name="primarySolver">The solver to run initially.</param>
        /// <param name="secondarySolver">The solver to fall back to should <paramref name="primarySolver"/> fail.</param>
        public FallbackSolver(ISolver primarySolver, ISolver secondarySolver)
        {
            _primarySolver = primarySolver;
            _secondarySolver = secondarySolver;
        }

        /// <inheritdoc/>
        public Selections Solve(Requirements requirements)
        {
            Selections Handle(Exception ex)
            {
                Log.Info("Primary solver failed, falling back to secondary solver.");
                Log.Info(ex);

                try
                {
                    return _secondarySolver.Solve(requirements);
                }
                catch (WebException ex2)
                {
                    Log.Warn("Unable to download secondary solver");
                    Log.Info(ex2);

                    // Report the original problem instead of inability to launch secondary solver
                    throw ex.PreserveStack();
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
}
