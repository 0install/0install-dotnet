// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using NanoByte.Common.Threading;
using ZeroInstall.Model;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Icons
{
    /// <summary>
    /// Stores icon files downloaded from the web as local files.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    [PrimaryConstructor]
    public sealed partial class IconStore : IIconStore
    {
        private readonly string _path;
        private readonly Config _config;
        private readonly ITaskHandler _handler;
        private readonly JobQueue _backgroundUpdates = new();

        /// <summary>
        /// The maximum number of bytes to download for a single icon.
        /// </summary>
        private const long MaximumIconSize = 2 * 1024 * 1024; // 2MiB

        /// <inheritdoc/>
        public string? GetCached(Icon icon, out bool stale)
        {
            #region Sanity checks
            if (icon == null) throw new ArgumentNullException(nameof(icon));
            if (icon.Href == null) throw new ArgumentException("Missing href.", nameof(icon));
            #endregion

            string path = GetPath(icon);

            if (File.Exists(path))
            {
                stale = DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > _config.Freshness;
                return path;
            }
            else
            {
                stale = false;
                return null;
            }
        }

        /// <summary>
        /// Gets an icon from the cache or downloads it if it is missing or stale/outdated.
        /// </summary>
        /// <param name="icon">The icon to get.</param>
        /// <param name="backgroundUpdate">Set to <c>true</c> to return stale icons and download an update in the background for future use.</param>
        /// <returns>The file path of the cached icon.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while adding the icon to the cache.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to the cache is not permitted.</exception>
        /// <exception cref="WebException">A problem occurred while downloading the icon.</exception>
        public string Get(Icon icon, bool backgroundUpdate = false)
        {
            // Prevent concurrent downloads of same icon
            using var mutex = new MutexLock("ZeroInstall.Model.Icon." + GetPath(icon).GetHashCode());

            string path = GetCached(icon, out bool stale) ?? Download(icon);

            void Update()
            {
                try
                {
                    Download(icon);
                }
                catch (Exception ex) when (ex is WebException or IOException or UnauthorizedAccessException)
                {
                    Log.Warn(ex);
                }
            }

            if (stale && _config.NetworkUse == NetworkLevel.Full && NetUtils.IsInternetConnected)
            {
                if (backgroundUpdate) _backgroundUpdates.Enqueue(Update);
                else Update();
            }

            return path;
        }

        private string Download(Icon icon)
        {
            if (_config.NetworkUse == NetworkLevel.Offline)
                throw new WebException(string.Format(Resources.NoDownloadInOfflineMode, icon.Href));

            string path = GetPath(icon);

            using var atomic = new AtomicWrite(path);
            _handler.RunTask(new DownloadFile(icon.Href, atomic.WritePath) {BytesMaximum = MaximumIconSize});
            atomic.Commit();

            return path;
        }

        internal string GetPath(Icon icon)
        {
            string path = Path.Combine(_path, FeedUri.Escape(icon.Href.AbsoluteUri));

            void EnsureExtension(string mimeType, string extension)
            {
                if (icon.MimeType == mimeType && !StringUtils.EqualsIgnoreCase(Path.GetExtension(path), extension))
                    path += extension;
            }

            EnsureExtension(Icon.MimeTypePng, ".png");
            EnsureExtension(Icon.MimeTypeIco, ".ico");
            EnsureExtension(Icon.MimeTypeSvg, ".svg");

            return path;
        }
    }
}
