// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using ZeroInstall.Store.Model;

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
        public static readonly RetrievalMethodRanker Instance = new RetrievalMethodRanker();

        private RetrievalMethodRanker() {}
        #endregion

        /// <inheritdoc/>
        public int Compare(RetrievalMethod x, RetrievalMethod y)
        {
            if (x == y)
                return 0;
            if (x is DownloadRetrievalMethod && y is Recipe)
                return -1;
            if (x is Recipe && y is DownloadRetrievalMethod)
                return 1;
            if (x is DownloadRetrievalMethod downloadX && y is DownloadRetrievalMethod downloadY)
                return downloadX.Size.CompareTo(downloadY.Size);
            if (x is Recipe recipeX && y is Recipe recipeY)
                return recipeX.Steps.Count.CompareTo(recipeY.Steps.Count);
            return 0;
        }
    }
}
