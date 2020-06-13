// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// A demand used by <see cref="ISolver"/>s internally. Wrapper for <see cref="Requirements"/> that holds <see cref="SelectionCandidate"/>s.
    /// </summary>
    public class SolverDemand
    {
        /// <summary>
        /// The requirements.
        /// </summary>
        public Requirements Requirements { get; }

        /// <summary>
        /// All candidates for the <see cref="Requirements"/>, including those that are not suitable.
        /// </summary>
        public IReadOnlyList<SelectionCandidate> Candidates { get; }

        /// <summary>
        /// Describes how important the demand is (i.e. whether ignoring it is an option).
        /// </summary>
        public Importance Importance { get; }

        /// <summary>
        /// Creates a new solver demand.
        /// </summary>
        /// <param name="requirements">The requirements.</param>
        /// <param name="candidateProvider">Generates <see cref="SelectionCandidate"/>s for the <paramref name="requirements"/>.</param>
        /// <param name="importance">Describes how important the demand is (i.e. whether ignoring it is an option).</param>
        public SolverDemand(Requirements requirements, ISelectionCandidateProvider candidateProvider, Importance importance = Importance.Essential)
        {
            Requirements = requirements ?? throw new ArgumentNullException(nameof(requirements));
            Candidates = (candidateProvider ?? throw new ArgumentNullException(nameof(candidateProvider))).GetSortedCandidates(Requirements);
            Importance = importance;
        }

        /// <summary>
        /// Returns the string representation of <see cref="Requirements"/>. Not safe for parsing!
        /// </summary>
        public override string ToString() => Requirements.ToString();
    }
}
