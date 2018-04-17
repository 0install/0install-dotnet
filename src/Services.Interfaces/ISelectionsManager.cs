// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;
using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Services
{
    /// <summary>
    /// Provides methods for filtering <see cref="Selections"/>.
    /// </summary>
    public interface ISelectionsManager
    {
        /// <summary>
        /// Returns a list of any downloadable <see cref="ImplementationSelection"/>s that are missing from an <see cref="IStore"/>.
        /// </summary>
        /// <param name="selections">The selections to search for <see cref="ImplementationSelection"/>s that are missing.</param>
        /// <remarks>Feed files may be downloaded, no implementations are downloaded.</remarks>
        /// <exception cref="KeyNotFoundException">A <see cref="Feed"/> or <see cref="Implementation"/> is missing.</exception>
        /// <exception cref="IOException">A problem occurred while reading the feed file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the cache is not permitted.</exception>
        /// <exception cref="InvalidDataException">The feed file could not be parsed.</exception>
        [NotNull, ItemNotNull]
        IEnumerable<ImplementationSelection> GetUncachedSelections([NotNull] Selections selections);

        /// <summary>
        /// Retrieves the original <see cref="Implementation"/>s these selections were based on.
        /// </summary>
        /// <param name="selections">The <see cref="ImplementationSelection"/>s to map back to <see cref="Implementation"/>s.</param>
        [NotNull, ItemNotNull]
        IEnumerable<Implementation> GetImplementations([NotNull] IEnumerable<ImplementationSelection> selections);

        /// <summary>
        /// Generates a tree representation of the dependencies within the selections.
        /// </summary>
        [NotNull]
        IEnumerable<SelectionsTreeNode> GetTree([NotNull] Selections selections);

        /// <summary>
        /// Generates a list of differences between two selections.
        /// </summary>
        /// <param name="oldSelections">The old selections to base the comparison on.</param>
        /// <param name="newSelections">The new selections to compare against.</param>
        [NotNull]
        IEnumerable<SelectionsDiffNode> GetDiff([NotNull] Selections oldSelections, [NotNull] Selections newSelections);
    }
}
