// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Net;
using System.Threading;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Tasks;
using NanoByte.Common.Threading;
using ZeroInstall.Archives;
using ZeroInstall.Model;
using ZeroInstall.Services.Native;
using ZeroInstall.Services.Properties;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Fetchers
{
    /// <summary>
    /// Downloads <see cref="Implementation"/>s, extracts them and adds them to an <see cref="IImplementationStore"/>.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public class Fetcher : FetcherBase
    {
        private readonly Config _config;

        /// <summary>
        /// Creates a new fetcher.
        /// </summary>
        /// <param name="config">User settings controlling network behaviour, solving, etc.</param>
        /// <param name="implementationStore">The location to store the downloaded and unpacked <see cref="Implementation"/>s in.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about progress.</param>
        public Fetcher(Config config, IImplementationStore implementationStore, ITaskHandler handler)
            : base(implementationStore, handler)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public override string? Fetch(Implementation implementation)
        {
            #region Sanity checks
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            #endregion

            // Use mutex to detect in-progress download of same implementation in other processes
            using var mutex = new Mutex(false, "0install-fetcher-" + GetDownloadID(implementation));
            try
            {
                while (!mutex.WaitOne(100, exitContext: false)) // NOTE: Might be blocked more than once
                    Handler.RunTask(new WaitTask(Resources.WaitingForDownload, mutex) {Tag = implementation.ManifestDigest.Best});
            }
            #region Error handling
            catch (AbandonedMutexException ex)
            {
                // Abandoned mutexes also get owned, but indicate something may have gone wrong elsewhere
                Log.Warn(ex);
            }
            #endregion

            try
            {
                // Check if another process added the implementation in the meantime
                string? path = GetPathSafe(implementation);
                if (path != null) return path;

                if (implementation.RetrievalMethods.Count == 0) throw new NotSupportedException(string.Format(Resources.NoRetrievalMethod, implementation.ID));
                Retrieve(implementation);

                return GetPathSafe(implementation);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Returns a unique identifier for an <see cref="Implementation"/>. Usually based on <see cref="ImplementationBase.ManifestDigest"/>.
        /// </summary>
        /// <exception cref="NotSupportedException"><paramref name="implementation"/> does not specify manifest digests in any known formats.</exception>
        private static string GetDownloadID(Implementation implementation)
            => implementation.ID.StartsWith(ExternalImplementation.PackagePrefix)
                ? implementation.ID
                : implementation.ManifestDigest.Best ?? implementation.ID;

        /// <inheritdoc/>
        protected override void Download(IBuilder builder, DownloadRetrievalMethod download, object? tag)
        {
            try
            {
                base.Download(builder, download, tag);
            }
            catch (WebException ex) when (!download.Href.IsLoopback && _config.FeedMirror != null)
            {
                Log.Warn(ex);
                Log.Info("Trying mirror");

                try
                {
                    Handler.RunTask(new DownloadFile(
                        new($"{_config.FeedMirror.EnsureTrailingSlash().AbsoluteUri}archive/{download.Href.Scheme}/{download.Href.Host}/{string.Concat(download.Href.Segments).TrimStart('/').Replace("/", "%23")}"),
                        stream => builder.Add(download, stream, Handler, tag),
                        download.DownloadSize)
                    {Tag = tag});
                }
                catch (WebException)
                {
                    // Report the original problem instead of mirror errors
                    throw ex.Rethrow();
                }
            }
        }
    }
}
