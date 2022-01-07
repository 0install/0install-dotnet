// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Provides <see cref="IIconStore"/> instances.
    /// </summary>
    public static class IconStores
    {
        /// <summary>
        /// Creates an <see cref="IIconStore"/> for use in a Catalog GUI.
        /// </summary>
        public static IIconStore Catalog(Config config, ITaskHandler handler)
            => new IconStore(
                Locations.GetCacheDirPath("0install.net", false, "icons"),
                config,
                handler);

        /// <summary>
        /// Creates an <see cref="IIconStore"/> for use with desktop integration.
        /// </summary>
        public static IIconStore DesktopIntegration(Config config, ITaskHandler handler, bool machineWide)
            => new IconStore(
                IntegrationManager.GetDir(machineWide, "icons"),
                config,
                handler);
    }
}
