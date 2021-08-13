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
    /// Contains control logic for applying <see cref="Model.Capabilities.DefaultProgram"/> and <see cref="AccessPoints.DefaultProgram"/> on GNOME or KDE systems.
    /// </summary>
    public static class DefaultProgram
    {
        #region Register
        /// <summary>
        /// Registers an application as a candidate for a default program for some service in the current system.
        /// </summary>
        /// <param name="target">The application being integrated.</param>
        /// <param name="defaultProgram">The default program information to be registered.</param>
        /// <param name="machineWide">Apply the registration machine-wide instead of just for the current user.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <param name="accessPoint">Indicates that the program should be set as the current default for the service it provides.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="defaultProgram"/> is invalid.</exception>
        public static void Register(FeedTarget target, Model.Capabilities.DefaultProgram defaultProgram, IIconStore iconStore, bool machineWide, bool accessPoint = false)
        {
            #region Sanity checks
            if (defaultProgram == null) throw new ArgumentNullException(nameof(defaultProgram));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            if (string.IsNullOrEmpty(defaultProgram.ID)) throw new InvalidDataException("Missing ID");
            if (string.IsNullOrEmpty(defaultProgram.Service)) throw new InvalidDataException("Missing Service");

            // TODO: Implement
        }
        #endregion

        #region Unregister
        /// <summary>
        /// Unregisters an application as a candidate for a default program in the current system. This can only be applied machine-wide, not per user.
        /// </summary>
        /// <param name="defaultProgram">The default program information to be removed.</param>
        /// <param name="machineWide">Apply the registration machine-wide instead of just for the current user.</param>
        /// <param name="accessPoint">Indicates that the program was set as the current default for the service it provides.</param>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="defaultProgram"/> is invalid.</exception>
        public static void Unregister(Model.Capabilities.DefaultProgram defaultProgram, bool machineWide, bool accessPoint = false)
        {
            #region Sanity checks
            if (defaultProgram == null) throw new ArgumentNullException(nameof(defaultProgram));
            #endregion

            if (string.IsNullOrEmpty(defaultProgram.ID)) throw new InvalidDataException("Missing ID");
            if (string.IsNullOrEmpty(defaultProgram.Service)) throw new InvalidDataException("Missing Service");

            // TODO: Implement
        }
        #endregion
    }
}
