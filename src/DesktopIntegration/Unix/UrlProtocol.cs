// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using ZeroInstall.Model;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.Unix
{
    /// <summary>
    /// Contains control logic for applying <see cref="Model.Capabilities.UrlProtocol"/> and <see cref="AccessPoints.UrlProtocol"/> on GNOME systems.
    /// </summary>
    public static class UrlProtocol
    {
        #region Register
        /// <summary>
        /// Registers a URL protocol in the current system.
        /// </summary>
        /// <param name="target">The application being integrated.</param>
        /// <param name="urlProtocol">The URL protocol to register.</param>
        /// <param name="machineWide">Register the URL protocol machine-wide instead of just for the current user.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <param name="accessPoint">Indicates that the handler shall become the default handler for the protocol.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="urlProtocol"/> is invalid.</exception>
        public static void Register(FeedTarget target, Model.Capabilities.UrlProtocol urlProtocol, IIconStore iconStore, bool machineWide, bool accessPoint = false)
        {
            #region Sanity checks
            if (urlProtocol == null) throw new ArgumentNullException(nameof(urlProtocol));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            if (string.IsNullOrEmpty(urlProtocol.ID)) throw new InvalidDataException("Missing ID");

            // TODO: Implement
        }
        #endregion

        #region Unregister
        /// <summary>
        /// Unregisters a URL protocol in the current system.
        /// </summary>
        /// <param name="urlProtocol">The URL protocol to remove.</param>
        /// <param name="machineWide">Unregister the URL protocol machine-wide instead of just for the current user.</param>
        /// <param name="accessPoint">Indicates that the handler was the default handler for the protocol.</param>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="urlProtocol"/> is invalid.</exception>
        public static void Unregister(Model.Capabilities.UrlProtocol urlProtocol, bool machineWide, bool accessPoint = false)
        {
            #region Sanity checks
            if (urlProtocol == null) throw new ArgumentNullException(nameof(urlProtocol));
            #endregion

            if (string.IsNullOrEmpty(urlProtocol.ID)) throw new InvalidDataException("Missing ID");

            // TODO: Implement
        }
        #endregion
    }
}
