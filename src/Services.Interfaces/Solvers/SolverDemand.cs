// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// A demand used by <see cref="ISolver"/>s internally. Wrapper for <see cref="Requirements"/> that holds <see cref="SelectionCandidate"/>s.
    /// </summary>
    public record SolverDemand(Requirements Requirements, ISelectionCandidateProvider CandidateProvider, Importance Importance = Importance.Essential)
    {
        /// <summary>
        /// All candidates for the <see cref="Requirements"/>, including those that are not suitable.
        /// </summary>
        public IReadOnlyList<SelectionCandidate> Candidates { get; } = CandidateProvider.GetSortedCandidates(Requirements);
    }
}
