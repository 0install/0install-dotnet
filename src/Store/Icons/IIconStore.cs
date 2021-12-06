// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using ZeroInstall.Model;

namespace ZeroInstall.Store.Icons
{
    /// <summary>
    /// Stores icon files downloaded from the web as local files.
    /// </summary>
    /// <remarks>Implementations of this interface are immutable and thread-safe.</remarks>
    public interface IIconStore
    {
        /// <summary>
        /// Tries to get an icon that is already cached.
        /// </summary>
        /// <param name="icon">The icon to get.</param>
        /// <param name="stale">Indicates whether the cached file is outdated.</param>
        /// <returns>The file path of the cached icon; <c>null</c> if the icon is not cached yet.</returns>
        string? GetCached(Icon icon, out bool stale);

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
        string Get(Icon icon, bool backgroundUpdate = false);
    }
}
