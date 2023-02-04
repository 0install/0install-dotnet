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
            catch (WebException ex)
            {
                Log.Warn(ex);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Warn($"Failed to save feed {feedUri} in cache", ex);
            }
            finally
            {
                feedManager.Refresh = false;
            }
        }

        return feed;
    }

    /// <summary>
    /// Imports a local copy of a remote <see cref="Feed"/> into the <see cref="IFeedCache"/> after verifying its signature.
    /// </summary>
    /// <param name="feedManager">The <see cref="IFeedManager"/> implementation.</param>
    /// <param name="path">The path of a local copy of the feed.</param>
    /// <exception cref="IOException">A problem occurred while reading the feed file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to the feed file or the cache is not permitted.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing an XML file.</exception>
    /// <exception cref="SignatureException">The signature data of the feed file could not be handled or no signatures were trusted.</exception>
    public static void ImportFeed(this IFeedManager feedManager, string path)
    {
        #region Sanity checks
        if (feedManager == null) throw new ArgumentNullException(nameof(feedManager));
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        #endregion

        using var stream = File.OpenRead(path);
        feedManager.ImportFeed(stream, keyCallback: id =>
        {
            // Find .gpg files places next to the feed file
            string keyPath = Path.Combine(Path.GetDirectoryName(path) ?? "", id + ".gpg");
            return File.Exists(keyPath) ? new(File.ReadAllBytes(keyPath)) : null;
        });
    }
}
