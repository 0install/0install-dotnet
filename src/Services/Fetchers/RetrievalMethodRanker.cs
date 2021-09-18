// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using ZeroInstall.Model;

namespace ZeroInstall.Services.Fetchers
{
    /// <summary>
    /// Compares <see cref="RetrievalMethod"/>s and sorts them from most to least preferred by <see cref="IFetcher"/>s.
    /// </summary>
    public sealed class RetrievalMethodRanker : IComparer<RetrievalMethod>
    {
        #region Singleton
        /// <summary>
        /// Singleton pattern.
        /// </summary>
        public static readonly RetrievalMethodRanker Instance = new();

        private RetrievalMethodRanker() {}
        #endregion

        /// <inheritdoc/>
        public int Compare(RetrievalMethod? x, RetrievalMethod? y)
            => x switch
            {
                _ when ReferenceEquals(x, y) => 0,
                DownloadRetrievalMethod _ when y is Recipe => -1,
                Recipe _ when y is DownloadRetrievalMethod => 1,
                DownloadRetrievalMethod downloadX when y is DownloadRetrievalMethod downloadY => downloadX.Size.CompareTo(downloadY.Size),
                Recipe recipeX when y is Recipe recipeY => recipeX.Steps.Count.CompareTo(recipeY.Steps.Count),
                _ => 0
            };
    }
}
