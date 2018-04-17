// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using JetBrains.Annotations;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Store
{
    /// <summary>
    /// Associates a <see cref="FeedUri"/> with the <see cref="Feed"/> data aquired from there.
    /// </summary>
    /// <remarks><see cref="Model.Feed.Uri"/> is only mandatory for remote feeds. This structure associates a <see cref="FeedUri"/> with all kinds of feeds, local and remote.</remarks>
    public struct FeedTarget
    {
        /// <summary>
        /// The URI or local path (must be absolute) the feed was aquired from.
        /// </summary>
        [NotNull]
        public readonly FeedUri Uri;

        /// <summary>
        /// The data aquired from <see cref="Uri"/>. <see cref="Model.Feed.Normalize"/> has already been called.
        /// </summary>
        [NotNull]
        public readonly Feed Feed;

        /// <summary>
        /// Creates a new feed target.
        /// </summary>
        /// <param name="uri">The URI or local path (must be absolute) to the feed.</param>
        /// <param name="feed">The data aquired from <paramref name="uri"/>. <see cref="Model.Feed.Normalize"/> has already been called.</param>
        public FeedTarget([NotNull] FeedUri uri, [NotNull] Feed feed)
        {
            Uri = uri;
            Feed = feed;
        }
    }
}
