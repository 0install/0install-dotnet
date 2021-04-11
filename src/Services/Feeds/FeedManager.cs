// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Model.Preferences;
using ZeroInstall.Services.Properties;
using ZeroInstall.Store;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds
{
    /// <summary>
    /// Provides access to remote and local <see cref="Feed"/>s. Handles downloading and signature verification.
    /// </summary>
    /// <remarks>This class performs in-memory caching of <see cref="Feed"/>s and <see cref="FeedPreferences"/>.</remarks>
    #pragma warning disable 8766
    public class FeedManager : IFeedManager
    {
        private readonly Config _config;
        private readonly IFeedCache _feedCache;
        private readonly ITrustManager _trustManager;
        private readonly ITaskHandler _handler;

        /// <summary>
        /// Creates a new feed manager.
        /// </summary>
        /// <param name="config">User settings controlling network behaviour, solving, etc.</param>
        /// <param name="feedCache">The disk-based cache to store downloaded <see cref="Feed"/>s.</param>
        /// <param name="trustManager">Methods for verifying signatures and user trust.</param>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        public FeedManager(Config config, IFeedCache feedCache, ITrustManager trustManager, ITaskHandler handler)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _feedCache = feedCache ?? throw new ArgumentNullException(nameof(feedCache));
            _trustManager = trustManager ?? throw new ArgumentNullException(nameof(trustManager));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));

            _feeds = new(feedUri =>
            {
                var feed = GetFeed(feedUri);
                feed.Normalize(feedUri);
                return feed;
            });
        }

        private bool _refresh;

        /// <summary>
        /// Set to <c>true</c> to re-download <see cref="Feed"/>s even if they are already in the <see cref="IFeedCache"/>.
        /// </summary>
        /// <remarks>Setting this to <c>true</c> implicitly also flushes the in-memory cache.</remarks>
        public bool Refresh
        {
            get => _refresh;
            set
            {
                _refresh = value;
                if (value) Clear();
            }
        }

        /// <inheritdoc/>
        public bool Stale { get; set; }

        /// <inheritdoc/>
        public bool ShouldRefresh
            => Stale && _config.NetworkUse == NetworkLevel.Full;

        private readonly TransparentCache<FeedUri, Feed> _feeds;

        /// <inheritdoc/>
        public Feed this[FeedUri feedUri]
            => _feeds[feedUri];

        private readonly TransparentCache<FeedUri, FeedPreferences> _preferences = new(FeedPreferences.LoadForSafe);

        /// <inheritdoc/>
        public FeedPreferences GetPreferences(FeedUri feedUri)
            => _preferences[feedUri];

        /// <summary>
        /// Returns a specific <see cref="Feed"/>. Automatically handles downloading and caching. Updates the <see cref="Stale"/> indicator.
        /// </summary>
        /// <param name="feedUri">The canonical ID used to identify the feed.</param>
        /// <returns>The parsed <see cref="Feed"/> object.</returns>
        /// <remarks><see cref="Feed"/>s are always served from the <see cref="IFeedCache"/> if possible, unless <see cref="Refresh"/> is set to <c>true</c>.</remarks>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="WebException">A problem occurred while fetching the feed file.</exception>
        /// <exception cref="IOException">A problem occurred while reading the feed file.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the cache is not permitted.</exception>
        /// <exception cref="SignatureException">The signature data of a remote feed file could not be verified.</exception>
        /// <exception cref="InvalidDataException"><see cref="Feed.Uri"/> is missing or does not match <paramref name="feedUri"/>.</exception>
        private Feed GetFeed(FeedUri feedUri)
        {
            if (feedUri.IsFromDistribution)
                throw new ArgumentException($"{feedUri.ToStringRfc()} is a virtual feed URI and therefore cannot be downloaded.");
            if (feedUri.IsFile)
                return XmlStorage.LoadXml<Feed>(feedUri.LocalPath);

            if (Refresh) Download(feedUri);
            else if (!_feedCache.Contains(feedUri))
            {
                // Do not download in offline mode
                if (_config.NetworkUse == NetworkLevel.Offline)
                    throw new WebException(string.Format(Resources.FeedNotCachedOffline, feedUri));

                // Try to download missing feed
                Download(feedUri);
            }

            try
            {
                return LoadCached(feedUri);
            }
            #region Error handling
            catch (KeyNotFoundException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion
        }

        /// <summary>
        /// Loads a <see cref="Feed"/> from the <see cref="_feedCache"/>.
        /// </summary>
        /// <param name="feedUri">The ID used to identify the feed. Must be an HTTP(S) URL.</param>
        /// <returns>The parsed <see cref="Feed"/> object.</returns>
        /// <exception cref="KeyNotFoundException">The requested <paramref name="feedUri"/> was not found in the cache.</exception>
        /// <exception cref="IOException">A problem occurred while reading the feed file.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the cache is not permitted.</exception>
        private Feed LoadCached(FeedUri feedUri)
        {
            try
            {
                var feed = _feedCache.GetFeed(feedUri);
                if (IsStale(feedUri)) Stale = true;
                return feed;
            }
            #region Error handling
            catch (InvalidDataException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            catch (KeyNotFoundException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion
        }

        /// <inheritdoc/>
        public bool IsStale(FeedUri feedUri)
        {
            if (IsCheckAttemptDelayed(feedUri ?? throw new ArgumentNullException(nameof(feedUri)))) return false;
            return (DateTime.UtcNow - _preferences[feedUri].LastChecked) > _config.Freshness;
        }

        /// <inheritdoc/>
        public bool RateLimit(FeedUri feedUri)
        {
            // Double-checked locking
            if (IsCheckAttemptDelayed(feedUri)) return true;
            using (new MutexLock("ZeroInstall.Services.Feeds.FeedManager.RateLimit." + feedUri.GetHashCode()))
            {
                if (IsCheckAttemptDelayed(feedUri)) return true;
                SetLastCheckAttempt(feedUri);
            }

            return false;
        }

        private static readonly TimeSpan _checkAttemptDelay = TimeSpan.FromHours(1);

        private static void SetLastCheckAttempt(FeedUri feedUri)
            => FileUtils.Touch(GetLastCheckAttemptPath(feedUri));

        private static bool IsCheckAttemptDelayed(FeedUri feedUri)
        {
            var file = new FileInfo(GetLastCheckAttemptPath(feedUri));
            return file.Exists && ((DateTime.UtcNow - file.LastWriteTimeUtc) <= _checkAttemptDelay);
        }

        private static string GetLastCheckAttemptPath(FeedUri feedUri)
            => Path.Combine(
                Locations.GetCacheDirPath("0install.net", false, "injector", "last-check-attempt"),
                feedUri.PrettyEscape());

        /// <summary>
        /// Downloads a <see cref="Feed"/> into the <see cref="_feedCache"/> validating its signatures. Automatically falls back to the mirror server.
        /// </summary>
        /// <param name="feedUri">The URL of the feed to download.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="WebException">A problem occurred while fetching the feed file.</exception>
        /// <exception cref="IOException">A problem occurred while writing the feed file.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the cache is not permitted.</exception>
        /// <exception cref="SignatureException">The signature data of the feed file could not be handled or no signatures were trusted.</exception>
        /// <exception cref="UriFormatException"><see cref="Feed.Uri"/> is missing or does not match <paramref name="feedUri"/> or <paramref name="feedUri"/> is a local file.</exception>
        private void Download(FeedUri feedUri)
        {
            SetLastCheckAttempt(feedUri);

            try
            {
                var download = new DownloadMemory(feedUri);
                _handler.RunTask(download);
                ImportFeed(download.GetData(), feedUri);
            }
            catch (WebException ex) when (!feedUri.IsLoopback && _config.FeedMirror != null)
            {
                if (_handler.Verbosity == Verbosity.Batch)
                    Log.Info(string.Format(Resources.FeedDownloadError, feedUri) + " " + Resources.TryingFeedMirror);
                else
                    Log.Warn(string.Format(Resources.FeedDownloadError, feedUri) + " " + Resources.TryingFeedMirror);
                try
                {
                    var download = new DownloadMemory(new($"{_config.FeedMirror.EnsureTrailingSlash().AbsoluteUri}feeds/{feedUri.Scheme}/{feedUri.Host}/{string.Concat(feedUri.Segments).TrimStart('/').Replace("/", "%23")}/latest.xml"))
                    {
                        NoCache = Refresh
                    };
                    _handler.RunTask(download);
                    ImportFeed(download.GetData(), feedUri);
                }
                catch (WebException)
                {
                    // Report the original problem instead of mirror errors
                    ex.Rethrow();
                }
            }
        }

        /// <inheritdoc/>
        public void ImportFeed(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            var feed = XmlStorage.LoadXml<Feed>(path);
            if (feed.Uri == null) throw new InvalidDataException(Resources.ImportNoSource);
            ImportFeed(File.ReadAllBytes(path), feed.Uri, path);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _feeds.Clear();
            _preferences.Clear();
        }

        /// <summary>
        /// Imports a <see cref="Feed"/> into the <see cref="IFeedCache"/> after verifying its signature.
        /// </summary>
        /// <param name="data">The content of the feed.</param>
        /// <param name="feedUri">The URI the feed originally came from.</param>
        /// <param name="localPath">The local file path the feed data came from. May be <c>null</c> for in-memory data.</param>
        /// <exception cref="IOException">A problem occurred while reading the feed file.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the feed file or the cache is not permitted.</exception>
        /// <exception cref="SignatureException">The signature data of the feed file could not be handled or no signatures were trusted.</exception>
        /// <exception cref="UriFormatException"><see cref="Feed.Uri"/> is missing or does not match <paramref name="feedUri"/> or <paramref name="feedUri"/> is a local file.</exception>
        private void ImportFeed(byte[] data, FeedUri feedUri, string? localPath = null)
        {
            Log.Debug("Importing feed " + feedUri.ToStringRfc() + " from " + (localPath ?? "web"));

            CheckFeed(data, feedUri);
            CheckTrust(data, feedUri, localPath);
            AddToCache(data, feedUri);
        }

        private static void CheckFeed(byte[] data, FeedUri feedUri)
        {
            // Detect feed substitution
            var feed = XmlStorage.LoadXml<Feed>(new MemoryStream(data));
            if (feed.Uri == null) throw new InvalidDataException(string.Format(Resources.FeedUriMissing, feedUri));
            if (feed.Uri != feedUri) throw new InvalidDataException(string.Format(Resources.FeedUriMismatch, feed.Uri, feedUri));
        }

        private void CheckTrust(byte[] data, FeedUri feedUri, string? localPath)
        {
            // Detect replay attacks
            var newSignature = _trustManager.CheckTrust(data, feedUri, localPath);
            try
            {
                var oldSignature = _feedCache.GetSignatures(feedUri).OfType<ValidSignature>().FirstOrDefault();
                if (oldSignature != null && newSignature.Timestamp < oldSignature.Timestamp)
                    throw new ReplayAttackException(feedUri, oldSignature.Timestamp, newSignature.Timestamp);
            }
            catch (KeyNotFoundException)
            {
                // No existing feed to be replaced
            }
        }

        private void AddToCache(byte[] data, FeedUri feedUri)
        {
            _feedCache.Add(feedUri, data);

            var preferences = _preferences[feedUri];
            preferences.LastChecked = DateTime.UtcNow;
            preferences.Normalize();
            preferences.SaveFor(feedUri);
        }
    }
}
