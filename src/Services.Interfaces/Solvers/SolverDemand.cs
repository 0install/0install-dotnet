/*
 * Copyright 2010-2017 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

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
        [NotNull]
        public Requirements Requirements { get; }

        /// <summary>
        /// All candidates for the <see cref="Requirements"/>, including those that are not suitable.
        /// </summary>
        [NotNull, ItemNotNull]
        public IList<SelectionCandidate> Candidates { get; }

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
        public SolverDemand([NotNull] Requirements requirements, [NotNull] ISelectionCandidateProvider candidateProvider, Importance importance = Importance.Essential)
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
