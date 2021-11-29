// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Store.Configuration;

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

        /// <inheritdoc/>
        public string Download(Icon icon)
        {
            #region Sanity checks
            if (icon == null) throw new ArgumentNullException(nameof(icon));
            if (icon.Href == null) throw new ArgumentException("Missing href.", nameof(icon));
            #endregion

            string path = GetPath(icon);

            using var atomic = new AtomicWrite(path);
            _handler.RunTask(new DownloadFile(icon.Href, atomic.WritePath) { BytesMaximum = MaximumIconSize });
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
