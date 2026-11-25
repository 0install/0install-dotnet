// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration.Windows;

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

        string filePath = GetStartMenuPath(menuEntry.Category, menuEntry.Name, machineWide);
        Create(filePath, target, menuEntry.Command, iconStore);
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

        RemoveEmptyCategoryDirs(GetStartMenuCategoryPath(null, machineWide), menuEntry.Category);
    }

    /// <summary>
    /// Deletes the directory for a <paramref name="category"/> and its parent categories if they are empty. Never deletes <paramref name="menuDir"/> itself.
    /// </summary>
    /// <param name="menuDir">The path of the start menu programs folder.</param>
    /// <param name="category">The name of the category/directory below <paramref name="menuDir"/>; must already be validated; can be <c>null</c>.</param>
    internal static void RemoveEmptyCategoryDirs(string menuDir, string? category)
    {
        if (string.IsNullOrEmpty(category)) return;

        // Build list of directories from outermost to innermost
        var dirPaths = new List<string>();
        string dirPath = menuDir;
        foreach (string segment in category.Split('/', '\\'))
        {
            // Skip segments that do not add a directory level (e.g., from entries created under older or other rules), so menuDir itself is never in the list
            if (segment is "" or ".") continue;

            dirPath = Paths.Combine(dirPath, segment);
            dirPaths.Add(dirPath);
        }

        for (int i = dirPaths.Count - 1; i >= 0; i--)
        {
            try
            {
                // Stop at the first directory that is missing or not empty, so parent directories that were not emptied by this removal are left alone
                if (!Directory.Exists(dirPaths[i]) || Directory.GetFileSystemEntries(dirPaths[i]).Length != 0) break;
                Directory.Delete(dirPaths[i], recursive: false);
            }
            #region Error handling
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Warn($"Failed to delete empty start menu directory '{dirPaths[i]}'", ex);
                break;
            }
            #endregion
        }
    }

    /// <summary>
    /// Builds a path for a shortcut in the start menu programs folder, optionally appending a category.
    /// </summary>
    /// <param name="category">The name of the category/directory below the programs folder; may be nested using slashes; can be <c>null</c>.</param>
    /// <param name="name">The name of the shortcut (without the .lnk ending).</param>
    /// <param name="machineWide"><c>true</c> to use the machine-wide start menu; <c>false</c> for the per-user variant.</param>
    /// <exception cref="IOException"><paramref name="name"/> or <paramref name="category"/> contains invalid characters.</exception>
    public static string GetStartMenuPath(string? category, string? name, bool machineWide)
        => Paths.Combine(GetStartMenuCategoryPath(category, machineWide), $"{name}.lnk");

    /// <summary>
    /// Returns the start menu programs folder path, optionally appending a category.
    /// </summary>
    /// <param name="category">The name of the category/directory below the programs folder; may be nested using slashes; can be <c>null</c>.</param>
    /// <param name="machineWide"><c>true</c> to use the machine-wide start menu; <c>false</c> for the per-user variant.</param>
    /// <exception cref="IOException"><paramref name="category"/> contains invalid characters.</exception>
    private static string GetStartMenuCategoryPath(string? category, bool machineWide)
    {
        string menuDir = WindowsUtils.GetFolderPath(machineWide ? Environment.SpecialFolder.CommonPrograms : Environment.SpecialFolder.Programs);
        if (string.IsNullOrEmpty(category)) return menuDir;

        // More lenient than SafeName.ValidatePath(), so that entries created under older or other rules can still be removed
        if (Paths.IsBreakout(category))
            throw new IOException(string.Format(Resources.NameInvalidChars, category));

        return Paths.Combine(menuDir, Paths.ToNative(category));
    }
}
