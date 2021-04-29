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

namespace ZeroInstall.Store
{
    /// <summary>
    /// Stores icon files downloaded from the web as local files.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public sealed class IconStore : IIconStore
    {
        private readonly Config _config;
        private readonly ITaskHandler _handler;
        private readonly string _path;

        /// <summary>
        /// Creates a new icon store.
        /// </summary>
        /// <param name="config">User settings controlling network behaviour.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about icon downloading.</param>
        /// <param name="path">The path of the directory used to store icon files. Leave <c>null</c> for the default cache location.</param>
        public IconStore(Config config, ITaskHandler handler, string? path = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _path = path ?? Locations.GetCacheDirPath("0install.net", false, "icons");
        }

        /// <summary>
        /// The maximum number of bytes to download for a single icon.
        /// </summary>
        private const long MaximumIconSize = 2 * 1024 * 1024;

        /// <inheritdoc/>
        public string GetPath(Icon icon)
        {
            #region Sanity checks
            if (icon == null) throw new ArgumentNullException(nameof(icon));
            if (icon.Href == null) throw new ArgumentException("Missing href.", nameof(icon));
            #endregion

            string path = BuildPath(icon);

            using (new MutexLock("ZeroInstall.DesktopIntegration.IconStore." + path.GetHashCode())) // Prevent concurrent updates of same icon
            {
                if (File.Exists(path))
                { // Existing file
                    if (_config.NetworkUse == NetworkLevel.Full && NetUtils.IsInternetConnected && IsFresh(path))
                    { // Outdated
                        try
                        {
                            Download(icon.Href, path);
                        }
                        #region Error handling
                        catch (WebException ex)
                        { // Failure is not critical if there is already a cached file
                            Log.Warn(ex);
                        }
                        catch (IOException ex)
                        { // Failure is not critical if there is already a cached file
                            Log.Warn(ex);
                        }
                        #endregion
                    }
                }
                else
                { // No existing file
                    Download(icon.Href, path);
                }
            }

            return path;
        }

        internal string BuildPath(Icon icon)
        {
            string path = Path.Combine(_path, FeedUri.Escape(icon.Href!.AbsoluteUri));

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

        private void Download(Uri href, string path)
        {
            using var atomic = new AtomicWrite(path);
            _handler.RunTask(new DownloadFile(href, atomic.WritePath) {BytesMaximum = MaximumIconSize});
            atomic.Commit();
        }

        private bool IsFresh(string path)
            => DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > _config.Freshness;
    }
}
