// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using JetBrains.Annotations;
using NanoByte.Common;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Windows
{
    public static partial class Shortcut
    {
        /// <summary>
        /// Creates a new Windows shortcut on the desktop.
        /// </summary>
        /// <param name="desktopIcon">Information about the shortcut to be created.</param>
        /// <param name="target">The target the shortcut shall point to.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <param name="machineWide">Create the shortcut machine-wide instead of just for the current user.</param>
        public static void Create([NotNull] DesktopIcon desktopIcon, FeedTarget target, [NotNull] IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (desktopIcon == null) throw new ArgumentNullException(nameof(desktopIcon));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            string filePath = GetDesktopPath(desktopIcon.Name, machineWide);
            Create(filePath, target, desktopIcon.Command, iconStore, machineWide);
        }

        /// <summary>
        /// Removes a Windows shortcut from the desktop.
        /// </summary>
        /// <param name="desktopIcon">Information about the shortcut to be removed.</param>
        /// <param name="machineWide">The shortcut was created machine-wide instead of just for the current user.</param>
        public static void Remove([NotNull] DesktopIcon desktopIcon, bool machineWide)
        {
            #region Sanity checks
            if (desktopIcon == null) throw new ArgumentNullException(nameof(desktopIcon));
            #endregion

            string filePath = GetDesktopPath(desktopIcon.Name, machineWide);
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        /// <summary>
        /// Builds a path for a shortcut on the desktop.
        /// </summary>
        /// <param name="name">The name of the shortcut (without the .lnk ending).</param>
        /// <param name="machineWide"><c>true</c> to use the machine-wide desktop; <c>false</c> for the per-user variant.</param>
        /// <exception cref="IOException"><paramref name="name"/> contains invalid characters.</exception>
        [NotNull]
        public static string GetDesktopPath([NotNull] string name, bool machineWide)
        {
            CheckName(name);

            string desktopDir = machineWide
                ? RegistryUtils.GetString(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Common Desktop")
                : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            return Path.Combine(desktopDir, name + ".lnk");
        }
    }
}
