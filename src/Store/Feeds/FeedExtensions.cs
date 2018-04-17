// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Store.Feeds
{
    /// <summary>
    /// Contains extension methods for <see cref="Feed"/>s.
    /// </summary>
    public static class FeedExtensions
    {
        /// <summary>
        /// Tries to find an <see cref="Implementation"/> with a specific <see cref="ManifestDigest"/> in a list of <see cref="Feed"/>s.
        /// </summary>
        /// <param name="feeds">The list of <see cref="Feed"/>s to search in.</param>
        /// <param name="digest">The digest to search for.</param>
        /// <param name="feed">Returns the <see cref="Feed"/> a match was found in; <c>null</c> if no match found.</param>
        /// <returns>The matching <see cref="Implementation"/>; <c>null</c> if no match found.</returns>
        [ContractAnnotation("=>null,feed:null; =>notnull,feed:notnull")]
        public static Implementation GetImplementation([NotNull] this IEnumerable<Feed> feeds, ManifestDigest digest, out Feed feed)
        {
            #region Sanity checks
            if (feeds == null) throw new ArgumentNullException(nameof(feeds));
            #endregion

            foreach (var curFeed in feeds)
            {
                var impl = curFeed.Implementations.FirstOrDefault(implementation => implementation.ManifestDigest.PartialEquals(digest));
                if (impl != null)
                {
                    feed = curFeed;
                    return impl;
                }
            }

            feed = null;
            return null;
        }
    }
}
