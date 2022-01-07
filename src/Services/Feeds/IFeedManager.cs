// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using ZeroInstall.Model;
using ZeroInstall.Model.Preferences;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds
{
    /// <summary>
    /// Provides access to remote and local <see cref="Feed"/>s. Handles downloading and signature verification.
    /// </summary>
    public interface IFeedManager
    {
        /// <summary>
        /// Set to <c>true</c> to re-download <see cref="Feed"/>s even if they are already in the <see cref="IFeedCache"/>.
        /// </summary>
        bool Refresh { get; set; }

        /// <summary>
        /// Is set to <c>true</c> if any <see cref="Feed"/> returned by <see cref="this"/> is getting stale and should be updated by setting <see cref="Refresh"/> to <c>true</c>.
        /// </summary>
        /// <remarks><see cref="Config.Freshness"/> controls the time span after which a feed is considered stale.</remarks>
        bool Stale { get; set; }

        /// <summary>
        /// Indicates whether <see cref="Stale"/> is <c>true</c> and <see cref="Config.NetworkUse"/> is <see cref="NetworkLevel.Full"/>.
        /// </summary>
        bool ShouldRefresh { get; }

        /// <summary>
        /// Returns a specific <see cref="Feed"/>. Automatically handles downloading, calling <see cref="Feed.Normalize"/> and caching. Updates the <see cref="Stale"/> indicator.
        /// </summary>
        /// <param name="feedUri">The URI of the feed.</param>
        /// <returns>The normalized <see cref="Feed"/>. Do not modify! The same instance may be returned to future callers.</returns>
        /// <remarks><see cref="Feed"/>s are always served from the <see cref="IFeedCache"/> if possible, unless <see cref="Refresh"/> is set to <c>true</c>.</remarks>
        /// <exception cref="UriFormatException"><see cref="Feed.Uri"/> is missing or does not match <paramref name="feedUri"/>.</exception>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="WebException">A problem occurred while fetching the feed file.</exception>
        /// <exception cref="IOException">A problem occurred while reading the feed file.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the cache is not permitted.</exception>
        /// <exception cref="SignatureException">The signature data of a remote feed file could not be verified.</exception>
        /// <exception cref="InvalidDataException">A required property on the feed is not set or invalid.</exception>
        Feed this[FeedUri feedUri] { get; }

        /// <summary>
        /// Returns <see cref="FeedPreferences"/> for a specific feed.
        /// </summary>
        /// <param name="feedUri">The URI of the feed.</param>
        FeedPreferences GetPreferences(FeedUri feedUri);

        /// <summary>
        /// Determines whether there is a stale cached copy of a particular feed.
        /// </summary>
        /// <param name="feedUri">The URI of the feed.</param>
        /// <returns><c>true</c> if there is a stale copy in the cache or no copy at all; <c>false</c> if there is a fresh copy in the cache.</returns>
        bool IsStale(FeedUri feedUri);

        /// <summary>
        /// Ensures that operations regarding a specific feed do not happen to often in a row.
        /// </summary>
        /// <param name="feedUri">The URI of the feed.</param>
        /// <returns><c>true</c> if the operation should be skipped due to rate limiting; <c>false</c> if the operation may proceed.</returns>
        bool RateLimit(FeedUri feedUri);

        /// <summary>
        /// Imports a remote <see cref="Feed"/> into the <see cref="IFeedCache"/> after verifying its signature.
        /// </summary>
        /// <param name="path">The path of a local copy of the feed.</param>
        /// <exception cref="IOException">A problem occurred while reading the feed file.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the feed file or the cache is not permitted.</exception>
        /// <exception cref="SignatureException">The signature data of the feed file could not be handled or no signatures were trusted.</exception>
        /// <exception cref="InvalidDataException"><see cref="Feed.Uri"/> is missing.</exception>
        void ImportFeed(string path);

        /// <summary>
        /// Clears any in-memory caches.
        /// </summary>
        void Clear();
    }
}
