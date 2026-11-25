// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration.MacOS;

/// <summary>
/// Utility class for creating and modifying macOS Desktop Entries.
/// </summary>
public static class DesktopEntry
{
    public static void Create(MenuEntry menuEntry, FeedTarget target, IIconStore iconStore, bool machineWide)
    {
        // TODO: Implement
    }

    public static void Remove(MenuEntry menuEntry, bool machineWide)
    {
        // TODO: Implement
    }

    public static void Create(DesktopIcon desktopIcon, FeedTarget target, IIconStore iconStore, bool machineWide)
    {
        // TODO: Implement
    }

    public static void Remove(DesktopIcon desktopIcon, bool machineWide)
    {
        // TODO: Implement
    }

    public static void Create(AutoStart autoStart, FeedTarget target, IIconStore iconStore, bool machineWide)
    {
        // TODO: Implement
    }

    public static void Remove(AutoStart autoStart, bool machineWide)
    {
        // TODO: Implement
    }

    /// <summary>
    /// Updates databases used by the desktop environment to reflect changes to desktop integration.
    /// </summary>
    /// <param name="machineWide">Update the machine-wide databases instead of those of the current user.</param>
    internal static void UpdateDatabases(bool machineWide)
    {
        // TODO: Implement
    }
}
