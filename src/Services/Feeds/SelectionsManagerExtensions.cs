// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Feeds;

/// <summary>
/// Provides extension methods for <see cref="ISelectionsManager"/>.
/// </summary>
public static class SelectionsManagerExtensions
{
    /// <summary>
    /// Combines <see cref="ISelectionsManager.GetUncached"/> and <see cref="ISelectionsManager.GetImplementations"/>.
    /// </summary>
    /// <param name="selectionsManager">The <see cref="ISelectionsManager"/>.</param>
    /// <param name="selections">The selections to search for <see cref="ImplementationSelection"/>s that are missing.</param>
    public static List<Implementation> GetUncachedImplementations(this ISelectionsManager selectionsManager, Selections selections)
    {
        #region Sanity checks
        if (selectionsManager == null) throw new ArgumentNullException(nameof(selectionsManager));
        if (selections == null) throw new ArgumentNullException(nameof(selections));
        #endregion

        return selectionsManager.GetImplementations(selectionsManager.GetUncached(selections.Implementations)).ToList();
    }
}
