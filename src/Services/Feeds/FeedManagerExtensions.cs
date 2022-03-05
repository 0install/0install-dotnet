// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds;

/// <summary>
/// Provides extension methods for <see cref="IFeedManager"/>.
/// </summary>
public static class FeedManagerExtensions
{
    /// <summary>
    /// Returns a specific <see cref="Feed"/>. Automatically updates cached feeds when indicated by <see cref="IFeedManager.ShouldRefresh"/>.
    /// </summary>
    /// <param name="feedManager">The <see cref="IFeedManager"/> implementation.</param>
    /// <param name="feedUri">The canonical ID used to identify the feed.</param>
    /// <returns>The normalized <see cref="Feed"/>. Do not modify! The same instance may be returned to future callers.</returns>
    /// <remarks><see cref="Feed"/>s are always served from the <see cref="IFeedCache"/> if possible, unless <see cref="IFeedManager.Refresh"/> is set to <c>true</c>.</remarks>
    /// <exception cref="UriFormatException"><see cref="Feed.Uri"/> is missing or does not match <paramref name="feedUri"/>.</exception>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while reading the feed file.</exception>
    /// <exception cref="WebException">A problem occurred while fetching the feed file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to the cache is not permitted.</exception>
    /// <exception cref="SignatureException">The signature data of a remote feed file could not be verified.</exception>
    /// <exception cref="InvalidDataException">A required property on the feed is not set or invalid.</exception>
    public static Feed GetFresh(this IFeedManager feedManager, FeedUri feedUri)
    {
        #region Sanity checks
        if (feedManager == null) throw new ArgumentNullException(nameof(feedManager));
        if (feedUri == null) throw new ArgumentNullException(nameof(feedUri));
        #endregion

        var feed = feedManager[feedUri];

        if (feedManager is {Refresh: false, ShouldRefresh: true})
        {
            feedManager.Stale = false;
            feedManager.Refresh = true;
            try
            {
                feed = feedManager[feedUri];
            }
            catch (Exception ex) when (ex is WebException or IOException or UnauthorizedAccessException)
            {
                Log.Warn(ex);
            }
            finally
            {
                feedManager.Refresh = false;
            }
        }

        return feed;
    }

    /// <summary>
    /// Temporarily sets <see cref="IFeedManager.Refresh"/> to <c>false</c>.
    /// </summary>
    /// <returns>Call <see cref="IDisposable.Dispose"/> on this to restore the original value of <see cref="IFeedManager.Refresh"/>.</returns>
    public static IDisposable PauseRefresh(this IFeedManager feedManager)
    {
        bool backupRefresh = feedManager.Refresh;
        feedManager.Refresh = false;
        return new Disposable(() => feedManager.Refresh = backupRefresh);
    }
}
