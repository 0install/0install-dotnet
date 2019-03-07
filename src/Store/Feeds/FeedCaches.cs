// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Store.Feeds
{
    /// <summary>
    /// Provides <see cref="IFeedCache"/> instances.
    /// </summary>
    public static class FeedCaches
    {
        /// <summary>
        /// Creates an <see cref="IFeedCache"/> instance that uses the default cache location in the user profile.
        /// </summary>
        /// <param name="openPgp">Provides access to an encryption/signature system compatible with the OpenPGP standard.</param>
        /// <exception cref="IOException">A problem occurred while creating a directory.</exception>
        /// <exception cref="UnauthorizedAccessException">Creating a directory is not permitted.</exception>
        public static IFeedCache Default(IOpenPgp openPgp)
            => new DiskFeedCache(DefaultDirectoryPath, openPgp);

        /// <summary>
        /// The default feed cache location in the user profile.
        /// </summary>
        public static string DefaultDirectoryPath
            => Locations.GetCacheDirPath("0install.net", machineWide: false, resource: "interfaces");
    }
}
