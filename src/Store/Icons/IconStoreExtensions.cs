// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using ZeroInstall.Model;

namespace ZeroInstall.Store.Icons
{
    /// <summary>
    /// Provides extension methods for <see cref="IIconStore"/>.
    /// </summary>
    public static class IconStoreExtensions
    {
        /// <summary>
        /// Tries to get an icon that is already cached.
        /// </summary>
        /// <param name="iconStore">The icon store.</param>
        /// <param name="icon">The icon to get.</param>
        /// <returns>The file path of the cached icon; <c>null</c> if the icon is not cached yet.</returns>
        public static string? GetCached(this IIconStore iconStore, Icon icon)
            => iconStore.GetCached(icon, out _);

        /// <summary>
        /// Gets an icon from the cache or downloads it if it is missing or stale/outdated.
        /// </summary>
        /// <param name="iconStore">The icon store.</param>
        /// <param name="icon">The icon to get.</param>
        /// <returns>The file path of the cached icon.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while adding the icon to the cache.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to the cache is not permitted.</exception>
        /// <exception cref="WebException">A problem occurred while downloading the icon.</exception>
        public static string Get(this IIconStore iconStore, Icon icon)
            => iconStore.Get(icon, backgroundUpdate: false);
    }
}
