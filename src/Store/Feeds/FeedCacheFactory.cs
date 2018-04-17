// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Store.Feeds
{
    /// <summary>
    /// Creates <see cref="IFeedCache"/> instances.
    /// </summary>
    public static class FeedCacheFactory
    {
        /// <summary>
        /// Creates an <see cref="IFeedCache"/> instance that uses the default cache location in the user profile.
        /// </summary>
        /// <param name="openPgp">Provides access to an encryption/signature system compatible with the OpenPGP standard.</param>
        /// <exception cref="IOException">A problem occurred while creating a directory.</exception>
        /// <exception cref="UnauthorizedAccessException">Creating a directory is not permitted.</exception>
        public static IFeedCache CreateDefault(IOpenPgp openPgp)
            => new DiskFeedCache(Locations.GetCacheDirPath("0install.net", machineWide: false, resource: "interfaces"), openPgp);
    }
}
