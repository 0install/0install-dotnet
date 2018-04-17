// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using JetBrains.Annotations;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Unix
{
    /// <summary>
    /// Contains control logic for applying <see cref="AccessPoints.AppAlias"/> on Unix systems.
    /// </summary>
    public static class AppAlias
    {
        #region Create
        /// <summary>
        /// Creates an application alias in the current system.
        /// </summary>
        /// <param name="target">The application being integrated.</param>
        /// <param name="command">The command within <paramref name="target"/> the alias shall point to; can be <c>null</c>.</param>
        /// <param name="aliasName">The name of the alias to be created.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <param name="machineWide">Create the alias machine-wide instead of just for the current user.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurs while writing to the filesystem.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
        public static void Create(FeedTarget target, [CanBeNull] string command, [NotNull] string aliasName, [NotNull] IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(aliasName)) throw new ArgumentNullException(nameof(aliasName));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            if (string.IsNullOrEmpty(aliasName) || aliasName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                throw new IOException(string.Format(Resources.NameInvalidChars, aliasName));

            // TODO: Find directory in search PATH

            // TODO: Write file
        }
        #endregion

        #region Remove
        /// <summary>
        /// Removes an application alias from the current system.
        /// </summary>
        /// <param name="aliasName">The name of the alias to be removed.</param>
        /// <param name="machineWide">The alias was created machine-wide instead of just for the current user.</param>
        /// <exception cref="IOException">A problem occurs while writing to the filesystem.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
        public static void Remove(string aliasName, bool machineWide)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(aliasName)) throw new ArgumentNullException(nameof(aliasName));
            #endregion

            // TODO: Find directory in search PATH

            // TODO: Delete file
        }
        #endregion
    }
}
