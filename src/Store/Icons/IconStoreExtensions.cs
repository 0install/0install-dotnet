// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Threading;
using ZeroInstall.Model;

namespace ZeroInstall.Store.Icons
{
    /// <summary>
    /// Provides extension methods for <see cref="IIconStore"/>.
    /// </summary>
    public static class IconStoreExtensions
    {
        /// <summary>
        /// Gets an icon from the cache or downloads it if it is missing or stale/outdated.
        /// </summary>
        /// <param name="store">The store to get the icon from.</param>
        /// <param name="icon">The icon to get.</param>
        /// <returns>The file path of the cached icon.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while adding the icon to the cache.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to the cache is not permitted.</exception>
        /// <exception cref="WebException">A problem occurred while downloading the icon.</exception>
        public static string Get(this IIconStore store, Icon icon)
        {
            using var mutex = Mutex(icon);

            string path = store.GetCached(icon, out bool stale) ?? store.Download(icon);
            if (stale)
            {
                try
                {
                    return store.Download(icon);
                }
                catch (Exception ex) when (ex is WebException or IOException or UnauthorizedAccessException)
                {
                    Log.Warn(ex);
                }
            }
            return path;
        }

        /// <summary>
        /// Gets an icon from the cache or downloads it if it is missing. May return stale/outdated files.
        /// </summary>
        /// <param name="store">The store to get the icon from.</param>
        /// <param name="icon">The icon to get.</param>
        /// <param name="stale">Indicates whether the cached file is outdated.</param>
        /// <returns>The file path of the cached icon.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while adding the icon to the cache.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to the cache is not permitted.</exception>
        /// <exception cref="WebException">A problem occurred while downloading the icon.</exception>
        public static string Get(this IIconStore store, Icon icon, out bool stale)
        {
            using var mutex = Mutex(icon);

            return store.GetCached(icon, out stale) ?? store.Download(icon);
        }

        /// <summary>
        /// Prevents concurrent downloads of same icon.
        /// </summary>
        private static MutexLock Mutex(Icon icon) => new("ZeroInstall.Model.Icon." + icon.Href.GetHashCode());
    }
}
