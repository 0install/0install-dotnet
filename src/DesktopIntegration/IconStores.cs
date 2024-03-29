﻿// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Configuration;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Provides <see cref="IIconStore"/> instances.
/// </summary>
public static class IconStores
{
    /// <summary>
    /// Provides icon files for display in table- or tile-like GUIs. Files may be discarded later.
    /// </summary>
    /// <exception cref="IOException">A problem occurred while creating a directory.</exception>
    /// <exception cref="UnauthorizedAccessException">Creating a directory is not permitted.</exception>
    public static IIconStore Cache(Config config, ITaskHandler handler)
        => new IconStore(
            Locations.GetCacheDirPath("0install.net", false, "icons"),
            config,
            handler);

    /// <summary>
    /// Provides icon files for use with desktop integration. Files will remain persisted.
    /// </summary>
    /// <exception cref="IOException">A problem occurred while creating a directory.</exception>
    /// <exception cref="UnauthorizedAccessException">Creating a directory is not permitted.</exception>
    public static IIconStore DesktopIntegration(Config config, ITaskHandler handler, bool machineWide)
        => new IconStore(
            IntegrationManager.GetDir(machineWide, "icons"),
            config,
            handler);
}
