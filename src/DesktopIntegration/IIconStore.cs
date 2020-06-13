// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using ZeroInstall.Model;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Stores icon files downloaded from the web as local files.
    /// </summary>
    public interface IIconStore
    {
        /// <summary>
        /// Gets a specific icon from this cache. If the icon is missing it will be downloaded.
        /// </summary>
        /// <param name="icon">The icon to retrieve.</param>
        /// <param name="machineWide">Use a machine-wide cache directory instead of one just for the current user.</param>
        /// <returns>The local file path of the cached icon.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while adding the icon to the cache.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to the cache is not permitted.</exception>
        /// <exception cref="WebException">A problem occurred while downloading the icon.</exception>
        string GetPath(Icon icon, bool machineWide = false);
    }
}
