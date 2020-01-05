// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common;
using NanoByte.Common.Storage;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Windows
{
    public static partial class Shortcut
    {
        /// <summary>
        /// Creates a new Windows shortcut in the start menu or on the start page.
        /// </summary>
        /// <param name="menuEntry">Information about the shortcut to be created.</param>
        /// <param name="target">The target the shortcut shall point to.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <param name="machineWide">Create the shortcut machine-wide instead of just for the current user.</param>
        public static void Create(MenuEntry menuEntry, FeedTarget target, IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (menuEntry == null) throw new ArgumentNullException(nameof(menuEntry));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            string dirPath = GetStartMenuCategoryPath(menuEntry.Category, machineWide);
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            string filePath = GetStartMenuPath(menuEntry.Category, menuEntry.Name, machineWide);
            Create(filePath, target, menuEntry.Command, iconStore, machineWide);
        }

        /// <summary>
        /// Removes a Windows shortcut from the start menu or on the start page.
        /// </summary>
        /// <param name="menuEntry">Information about the shortcut to be removed.</param>
        /// <param name="machineWide">The shortcut was created machine-wide instead of just for the current user.</param>
        public static void Remove(MenuEntry menuEntry, bool machineWide)
        {
            #region Sanity checks
            if (menuEntry == null) throw new ArgumentNullException(nameof(menuEntry));
            #endregion

            string filePath = GetStartMenuPath(menuEntry.Category, menuEntry.Name, machineWide);
            if (File.Exists(filePath)) File.Delete(filePath);

            // Delete category directory if empty
            string dirPath = GetStartMenuCategoryPath(menuEntry.Category, machineWide);
            if (Directory.Exists(dirPath) && Directory.GetFileSystemEntries(dirPath).Length == 0)
                Directory.Delete(dirPath, recursive: false);
        }

        /// <summary>
        /// Builds a path for a shortcut in the start menu programs folder, optionally appending a category.
        /// </summary>
        /// <param name="category">The name of the category/directory below the programs folder; can be <c>null</c>.</param>
        /// <param name="name">The name of the shortcut (without the .lnk ending).</param>
        /// <param name="machineWide"><c>true</c> to use the machine-wide start menu; <c>false</c> for the per-user variant.</param>
        /// <exception cref="IOException"><paramref name="name"/> or <paramref name="category"/> contains invalid characters.</exception>
        public static string GetStartMenuPath(string? category, string name, bool machineWide)
        {
            CheckName(name);

            return Path.Combine(GetStartMenuCategoryPath(category, machineWide), name + ".lnk");
        }

        /// <summary>
        /// Returns the start menu programs folder path, optionally appending a category.
        /// </summary>
        /// <param name="category">The name of the category/directory below the programs folder; can be <c>null</c>.</param>
        /// <param name="machineWide"><c>true</c> to use the machine-wide start menu; <c>false</c> for the per-user variant.</param>
        /// <exception cref="IOException"><paramref name="category"/> contains invalid characters.</exception>
        private static string GetStartMenuCategoryPath(string? category, bool machineWide)
        {
            string menuDir = machineWide
                ? RegistryUtils.GetString(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Common Programs")
                : Environment.GetFolderPath(Environment.SpecialFolder.Programs);

            if (string.IsNullOrEmpty(category)) return menuDir;
            else
            {
                string categoryDir = FileUtils.UnifySlashes(category);
                if (categoryDir.IndexOfAny(Path.GetInvalidPathChars()) != -1 || FileUtils.IsBreakoutPath(categoryDir))
                    throw new IOException(string.Format(Resources.NameInvalidChars, category));

                return Path.Combine(menuDir, categoryDir);
            }
        }
    }
}
