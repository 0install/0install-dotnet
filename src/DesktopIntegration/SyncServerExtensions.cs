// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using JetBrains.Annotations;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Provides extension methods for <see cref="SyncServer"/>.
    /// </summary>
    public static class SyncServerExtensions
    {
        /// <summary>
        /// Reads the relevant information from a <see cref="Config"/> in order to construct a <see cref="SyncServer"/> struct.
        /// </summary>
        public static SyncServer ToSyncServer([NotNull] this Config config)
        {
            #region Sanity checks
            if (config == null) throw new ArgumentNullException(nameof(config));
            #endregion

            return new SyncServer {Uri = config.SyncServer, Username = config.SyncServerUsername, Password = config.SyncServerPassword};
        }

        /// <summary>
        /// Writes the data of a <see cref="SyncServer"/> struct back to a <see cref="Config"/>.
        /// </summary>
        public static void FromSyncServer([NotNull] this Config config, SyncServer syncServer)
        {
            #region Sanity checks
            if (config == null) throw new ArgumentNullException(nameof(config));
            #endregion

            config.SyncServer = new FeedUri(syncServer.Uri);
            config.SyncServerUsername = syncServer.Username;
            config.SyncServerPassword = syncServer.Password;
        }
    }
}
