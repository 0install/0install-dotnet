// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Associates a <see cref="FeedUri"/> with the <see cref="Feed"/> data acquired from there.
    /// </summary>
    /// <remarks><see cref="Model.Feed.Uri"/> is only mandatory for remote feeds. This structure associates a <see cref="FeedUri"/> with all kinds of feeds, local and remote.</remarks>
    public readonly struct FeedTarget
    {
        /// <summary>
        /// The URI or local path (must be absolute) the feed was acquired from.
        /// </summary>
        public readonly FeedUri Uri;

        /// <summary>
        /// The data acquired from <see cref="Uri"/>. <see cref="Model.Feed.Normalize"/> has already been called.
        /// </summary>
        public readonly Feed Feed;

        /// <summary>
        /// Creates a new feed target.
        /// </summary>
        /// <param name="uri">The URI or local path (must be absolute) to the feed.</param>
        /// <param name="feed">The data acquired from <paramref name="uri"/>. <see cref="Model.Feed.Normalize"/> has already been called.</param>
        public FeedTarget(FeedUri uri, Feed feed)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            Feed = feed ?? throw new ArgumentNullException(nameof(feed));
        }

        /// <summary>
        /// Returns the <see cref="Uri"/>. Not safe for parsing!
        /// </summary>
        public override string ToString()
            => Uri.ToStringRfc();
    }
}
