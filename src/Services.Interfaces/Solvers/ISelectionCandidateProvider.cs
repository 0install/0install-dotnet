// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Generates <see cref="SelectionCandidate"/>s for <see cref="ISolver"/>s to choose among.
    /// </summary>
    /// <remarks>Caches loaded <see cref="Feed"/>s, preferences, etc..</remarks>
    public interface ISelectionCandidateProvider
    {
        /// <summary>
        /// Gets all <see cref="SelectionCandidate"/>s for a specific set of <see cref="Requirements"/> sorted from best to worst.
        /// </summary>
        IReadOnlyList<SelectionCandidate> GetSortedCandidates(Requirements requirements);

        /// <summary>
        /// Retrieves the original <see cref="Implementation"/> an <see cref="ImplementationSelection"/> was based ofF.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The <paramref name="implementationSelection"/> was not provided by <see cref="GetSortedCandidates"/> or <see cref="Clear"/> was called in between.</exception>
        Implementation LookupOriginalImplementation(ImplementationSelection implementationSelection);

        /// <summary>
        /// Clears all internal caches.
        /// </summary>
        void Clear();
    }
}
