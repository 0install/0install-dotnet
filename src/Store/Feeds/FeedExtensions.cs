// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Feeds;

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
    /// <returns>The matching <see cref="Implementation"/> and the <see cref="Feed"/> it was found in; <c>null</c> if no match found.</returns>
    public static (Implementation implementation, Feed feed)? FindImplementation(this IEnumerable<Feed> feeds, ManifestDigest digest)
    {
        #region Sanity checks
        if (feeds == null) throw new ArgumentNullException(nameof(feeds));
        #endregion

        foreach (var curFeed in feeds)
        {
            var impl = curFeed.Implementations.FirstOrDefault(implementation => implementation.ManifestDigest.PartialEquals(digest));
            if (impl != null)
                return (impl, curFeed);
        }

        return null;
    }
}
