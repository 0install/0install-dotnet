// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Linq;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services
{
    /// <summary>
    /// Provides extension methods for <see cref="ISelectionsManager"/>.
    /// </summary>
    public static class SelectionsManagerExtensions
    {
        /// <summary>
        /// Combines <see cref="ISelectionsManager.GetUncachedSelections"/> and <see cref="ISelectionsManager.GetImplementations"/>.
        /// </summary>
        /// <param name="selectionsManager">The <see cref="ISelectionsManager"/></param>
        /// <param name="selections">The selections to search for <see cref="ImplementationSelection"/>s that are missing.</param>
        public static ICollection<Implementation> GetUncachedImplementations(this ISelectionsManager selectionsManager, Selections selections)
        {
            #region Sanity checks
            if (selectionsManager == null) throw new ArgumentNullException(nameof(selectionsManager));
            if (selections == null) throw new ArgumentNullException(nameof(selections));
            #endregion

            return selectionsManager.GetImplementations(selectionsManager.GetUncachedSelections(selections)).ToList();
        }
    }
}
