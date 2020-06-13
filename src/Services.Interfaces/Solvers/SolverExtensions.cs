// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using NanoByte.Common;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Provides extension methods for <see cref="ISolver"/>.
    /// </summary>
    public static class SolverExtensions
    {
        /// <summary>
        /// Provides <see cref="Selections"/> that satisfy a set of <see cref="Requirements"/>. Catches most exceptions and <see cref="Log"/>s them.
        /// </summary>
        /// <param name="solver">The <see cref="ISolver"/> implementation.</param>
        /// <param name="requirements">A set of requirements/restrictions imposed by the user on the implementation selection process.</param>
        /// <returns>The <see cref="ImplementationSelection"/>s chosen for the feed; <c>null</c> if there was a problem.</returns>
        /// <remarks>Feed files may be downloaded, signature validation is performed, implementations are not downloaded.</remarks>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="ArgumentException"><paramref name="requirements"/> is incomplete.</exception>
        public static Selections? TrySolve(this ISolver solver, Requirements requirements)
        {
            #region Sanity checks
            if (solver == null) throw new ArgumentNullException(nameof(solver));
            #endregion

            try
            {
                return solver.Solve(requirements);
            }
            #region Error handling
            catch (IOException ex)
            {
                Log.Warn(ex);
                return null;
            }
            catch (WebException ex)
            {
                Log.Warn(ex);
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warn(ex);
                return null;
            }
            catch (SignatureException ex)
            {
                Log.Warn(ex);
                return null;
            }
            catch (SolverException ex)
            {
                Log.Warn(ex);
                return null;
            }
            #endregion
        }
    }
}
