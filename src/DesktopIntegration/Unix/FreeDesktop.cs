// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Model;

namespace ZeroInstall.DesktopIntegration.Unix
{
    /// <summary>
    /// Utility class for creating and modifying FreeDesktop.org Desktop Entries.
    /// </summary>
    public static class FreeDesktop
    {
        public static void Create(MenuEntry menuEntry, FeedTarget target, IIconStore iconStore, bool machineWide) => throw new NotImplementedException();

        public static void Remove(MenuEntry menuEntry, bool machineWide) => throw new NotImplementedException();

        public static void Create(DesktopIcon desktopIcon, FeedTarget target, IIconStore iconStore, bool machineWide) => throw new NotImplementedException();

        public static void Remove(DesktopIcon desktopIcon, bool machineWide) => throw new NotImplementedException();
    }
}
