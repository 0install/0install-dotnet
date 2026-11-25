// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Utility class for creating and modifying freedesktop.org Desktop Entries.
/// </summary>
public static class FreeDesktop
{
    public static void Create(MenuEntry menuEntry, FeedTarget target, IIconStore iconStore, bool machineWide) => throw new NotImplementedException();

    public static void Remove(MenuEntry menuEntry, bool machineWide) => throw new NotImplementedException();

    public static void Create(DesktopIcon desktopIcon, FeedTarget target, IIconStore iconStore, bool machineWide) => throw new NotImplementedException();

    public static void Remove(DesktopIcon desktopIcon, bool machineWide) => throw new NotImplementedException();

    public static void Create(AutoStart autoStart, FeedTarget target, IIconStore iconStore, bool machineWide) => throw new NotImplementedException();

    public static void Remove(AutoStart autoStart, bool machineWide) => throw new NotImplementedException();

    #region Databases
    /// <summary>
    /// Updates databases used by desktop environments to reflect changes to Desktop Entries and MIME types.
    /// </summary>
    /// <param name="machineWide">Update the machine-wide databases instead of those of the current user.</param>
    internal static void UpdateDatabases(bool machineWide)
    {
        // TODO: Implement
    }
    #endregion
}
