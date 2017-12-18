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

using System.Collections.Generic;
using JetBrains.Annotations;
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
        [NotNull, ItemNotNull]
        IList<SelectionCandidate> GetSortedCandidates([NotNull] Requirements requirements);

        /// <summary>
        /// Retrieves the original <see cref="Implementation"/> an <see cref="ImplementationSelection"/> was based ofF.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The <paramref name="implemenationSelection"/> was not provided by <see cref="GetSortedCandidates"/> or <see cref="Clear"/> was called in between.</exception>
        [NotNull]
        Implementation LookupOriginalImplementation([NotNull] ImplementationSelection implemenationSelection);

        /// <summary>
        /// Clears all internal caches.
        /// </summary>
        void Clear();
    }
}