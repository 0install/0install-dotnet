// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Store.Icons
{
    /// <summary>
    /// Provides <see cref="IIconStore"/> instances.
    /// </summary>
    public static class IconStores
    {
        /// <summary>
        /// Creates an <see cref="IIconStore"/> instance that uses the default cache location.
        /// </summary>
        /// <exception cref="IOException">A problem occurred while creating a directory.</exception>
        /// <exception cref="UnauthorizedAccessException">Creating a directory is not permitted.</exception>
        public static IIconStore Default(ITaskHandler handler)
            => new IconStore(
                Locations.GetCacheDirPath("0install.net", false, "icons"),
                Config.LoadSafe(),
                handler);
    }
}
