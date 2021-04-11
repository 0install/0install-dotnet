// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Chooses a set of <see cref="Implementation"/>s to satisfy the requirements of a program and its user.
    /// </summary>
    /// <remarks>Implementations of this interface are immutable and thread-safe.</remarks>
    public interface ISolver
    {
        /// <summary>
        /// Provides <see cref="Selections"/> that satisfy a set of <see cref="Requirements"/>.
        /// </summary>
        /// <param name="requirements">The requirements to satisfy.</param>
        /// <returns>The selected <see cref="ImplementationSelection"/>s.</returns>
        /// <remarks>Feed files may be downloaded, signature validation is performed, implementations are not downloaded.</remarks>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="ArgumentException"><paramref name="requirements"/> is incomplete.</exception>
        /// <exception cref="IOException">A problem occurred while reading the feed file.</exception>
        /// <exception cref="WebException">A problem occurred while fetching the feed file.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the cache is not permitted.</exception>
        /// <exception cref="SignatureException">The signature data of a remote feed file could not be verified.</exception>
        /// <exception cref="SolverException">The solver was unable to provide <see cref="Selections"/> that fulfill the <paramref name="requirements"/>.</exception>
        Selections Solve(Requirements requirements);
    }
}
