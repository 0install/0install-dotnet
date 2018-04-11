/*
 * Copyright 2010-2018 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Net;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Stores icon files downloaded from the web as local files.
    /// </summary>
    public sealed class IconStore : IIconStore
    {
        [NotNull]
        private readonly ITaskHandler _handler;

        [CanBeNull]
        private readonly string _pathOverride;

        /// <summary>
        /// Creates a new icon cache.
        /// </summary>
        /// <param name="handler">A callback object used when the the user is to be informed about icon downloading.</param>
        /// <param name="pathOverride">An alternative on-disk path to use for storage. Only set for testing, leave <c>null</c> otherwise.</param>
        public IconStore([NotNull] ITaskHandler handler, [CanBeNull] string pathOverride = null)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _pathOverride = pathOverride;
        }

        private readonly object _lock = new object();

        /// <summary>How long to keep cached icons files before refreshing them.</summary>
        private static readonly TimeSpan _freshness = new TimeSpan(0, 20, 0); // 20 minutes

        /// <inheritdoc/>
        public string GetPath(Icon icon, bool machineWide = false)
        {
            string path = BuildPath(icon ?? throw new ArgumentNullException(nameof(icon)), machineWide);

            void Download()
            {
                using (var atomic = new AtomicWrite(path))
                {
                    _handler.RunTask(new DownloadFile(icon.Href, atomic.WritePath));
                    atomic.Commit();
                }
            }

            lock (_lock) // Prevent concurrent downloads
            {
                if (File.Exists(path))
                {
                    if (DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > _freshness && NetUtils.IsInternetConnected())
                    {
                        try
                        {
                            Download();
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
                else Download();
            }

            return path;
        }

        internal string BuildPath([NotNull] Icon icon, bool machineWide)
        {
            string path = Path.Combine(
                _pathOverride ?? Locations.GetIntegrationDirPath("0install.net", machineWide, "desktop-integration", "icons"),
                FeedUri.Escape(icon.Href.AbsoluteUri));

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
