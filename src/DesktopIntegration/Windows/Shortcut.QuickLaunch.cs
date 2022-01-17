// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration.Windows;

public static partial class Shortcut
{
    /// <summary>
    /// Creates a new Windows shortcut in the quick launch bar.
    /// </summary>
    /// <param name="quickLaunch">Information about the shortcut to be created.</param>
    /// <param name="target">The target the shortcut shall point to.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    public static void Create(QuickLaunch quickLaunch, FeedTarget target, IIconStore iconStore)
    {
        #region Sanity checks
        if (quickLaunch == null) throw new ArgumentNullException(nameof(quickLaunch));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        string filePath = GetQuickLaunchPath(quickLaunch.Name);
        Create(filePath, target, quickLaunch.Command, iconStore);
    }

    /// <summary>
    /// Removes a Windows shortcut from the quick launch bar.
    /// </summary>
    /// <param name="quickLaunch">Information about the shortcut to be removed.</param>
    public static void Remove(QuickLaunch quickLaunch)
    {
        #region Sanity checks
        if (quickLaunch == null) throw new ArgumentNullException(nameof(quickLaunch));
        #endregion

        string filePath = GetQuickLaunchPath(quickLaunch.Name);
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    private static string GetQuickLaunchPath(string? name)
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Internet Explorer", "Quick Launch", name + ".lnk");
}
