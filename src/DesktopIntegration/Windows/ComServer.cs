// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using ZeroInstall.Model;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains control logic for applying <see cref="Model.Capabilities.ComServer"/> on Windows systems.
    /// </summary>
    public static class ComServer
    {
        #region Constants
        /// <summary>The HKCR registry key for storing COM class IDs.</summary>
        public const string RegKeyClassesIDs = @"CLSID";
        #endregion

        #region Register
        /// <summary>
        /// Registers a COM server in the current system.
        /// </summary>
        /// <param name="target">The application being integrated.</param>
        /// <param name="comServer">The COM server to be registered.</param>
        /// <param name="machineWide">Register the COM server machine-wide instead of just for the current user.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="comServer"/> is invalid.</exception>
        public static void Register(FeedTarget target, Model.Capabilities.ComServer comServer, IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (comServer == null) throw new ArgumentNullException(nameof(comServer));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            if (string.IsNullOrEmpty(comServer.ID)) throw new InvalidDataException("Missing ID");

            // TODO: Implement
        }
        #endregion

        #region Unregister
        /// <summary>
        /// Unregisters a COM server in the current system.
        /// </summary>
        /// <param name="comServer">The COM server to be unregistered.</param>
        /// <param name="machineWide">Unregister the COM server machine-wide instead of just for the current user.</param>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="comServer"/> is invalid.</exception>
        public static void Unregister(Model.Capabilities.ComServer comServer, bool machineWide)
        {
            #region Sanity checks
            if (comServer == null) throw new ArgumentNullException(nameof(comServer));
            #endregion

            if (string.IsNullOrEmpty(comServer.ID)) throw new InvalidDataException("Missing ID");

            // TODO: Implement
        }
        #endregion
    }
}
