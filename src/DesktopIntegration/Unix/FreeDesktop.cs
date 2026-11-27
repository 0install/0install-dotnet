// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Utility class for creating and modifying freedesktop.org Desktop Entries.
/// </summary>
/// <seealso href="https://specifications.freedesktop.org/desktop-entry-spec/latest/"/>
public static class FreeDesktop
{
    /// <summary>
    /// Prepended to the names of generated files in shared directories. This prevents conflicts with files created by other applications.
    /// </summary>
    internal const string FilePrefix = "zeroinstall-";

    #region Directories
    /// <summary>
    /// Returns the directory for data files, such as Desktop Entries.
    /// </summary>
    internal static string GetDataDir(bool machineWide)
        => machineWide ? "/usr/local/share" : Locations.UserDataDir;

    /// <summary>
    /// Returns the directory for configuration files, such as auto-start entries.
    /// </summary>
    internal static string GetConfigDir(bool machineWide)
        => machineWide ? "/etc/xdg" : Locations.UserConfigDir;

    /// <summary>
    /// Returns the directory for Desktop Entries listed in application menus.
    /// </summary>
    internal static string GetApplicationsDir(bool machineWide)
        => Paths.Combine(GetDataDir(machineWide), "applications");
    #endregion

    #region Menu entries
    /// <summary>
    /// Creates a new Desktop Entry in the application menu.
    /// </summary>
    /// <param name="menuEntry">Information about the entry to be created.</param>
    /// <param name="target">The application being integrated.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    /// <param name="machineWide">Create the entry machine-wide instead of just for the current user.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Create(MenuEntry menuEntry, FeedTarget target, IIconStore iconStore, bool machineWide)
    {
        #region Sanity checks
        if (menuEntry == null) throw new ArgumentNullException(nameof(menuEntry));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        GetDesktopEntry(menuEntry, target, iconStore)
           .Add("Categories", GetCategories(menuEntry.Category, target.Feed))
           .Save(GetMenuEntryPath(menuEntry, machineWide));
    }

    /// <summary>
    /// Removes a Desktop Entry from the application menu.
    /// </summary>
    /// <param name="menuEntry">Information about the entry to be removed.</param>
    /// <param name="machineWide">The entry was created machine-wide instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Remove(MenuEntry menuEntry, bool machineWide)
    {
        #region Sanity checks
        if (menuEntry == null) throw new ArgumentNullException(nameof(menuEntry));
        #endregion

        KeyFile.Delete(GetMenuEntryPath(menuEntry, machineWide));
    }

    private static string GetMenuEntryPath(MenuEntry menuEntry, bool machineWide)
        => Paths.Combine(GetApplicationsDir(machineWide), $"{FilePrefix}menu-{FeedUri.Escape($"{menuEntry.Category}/{menuEntry.Name}")}.desktop");

    /// <summary>
    /// Determines the freedesktop.org menu categories to use for an application.
    /// </summary>
    /// <param name="menuCategory">The category/folder chosen for the menu entry; can be <c>null</c>.</param>
    /// <param name="feed">The feed of the application.</param>
    /// <remarks>Folders that do not match a well-known category are ignored, because freedesktop.org menus have a fixed structure.</remarks>
    internal static IReadOnlyCollection<string> GetCategories(string? menuCategory, Feed feed)
    {
        var categories = feed.Categories
                             .Where(x => string.IsNullOrEmpty(x.TypeNamespace))
                             .Select(x => x.Name ?? "")
                             .Where(x => x.Length != 0 && x.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-'))
                             .ToList();

        // Explicitly chosen main category takes precedence over main categories from feed
        string? chosen = menuCategory?.Split('/', '\\').FirstOrDefault(Category.WellKnownNames.Contains);
        if (chosen != null && !categories.Contains(chosen))
        {
            categories.RemoveAll(Category.WellKnownNames.Contains);
            categories.Insert(0, chosen);
        }

        // Required by specification
        if (categories.Any(x => x is "Audio" or "Video") && !categories.Contains("AudioVideo"))
            categories.Insert(0, "AudioVideo");

        return categories.Distinct().ToList();
    }
    #endregion

    #region Desktop icons
    /// <summary>
    /// Creates a new Desktop Entry on the desktop.
    /// </summary>
    /// <param name="desktopIcon">Information about the entry to be created.</param>
    /// <param name="target">The application being integrated.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    /// <param name="machineWide">Create the entry machine-wide instead of just for the current user. Not supported, will do nothing.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Create(DesktopIcon desktopIcon, FeedTarget target, IIconStore iconStore, bool machineWide)
    {
        #region Sanity checks
        if (desktopIcon == null) throw new ArgumentNullException(nameof(desktopIcon));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        if (machineWide)
        {
            Log.Info($"Skipping '{desktopIcon}' because there is no machine-wide desktop on freedesktop.org systems");
            return;
        }

        // Desktop environments only allow launching if marked as executable
        string path = GetDesktopIconPath(desktopIcon);
        GetDesktopEntry(desktopIcon, target, iconStore)
           .Save(path, executable: true);

        // GNOME also requires the file to be marked as trusted
        TryRun("gio", ["set", path, "metadata::trusted", "true"], warnOnFailure: false);
    }

    /// <summary>
    /// Removes a Desktop Entry from the desktop.
    /// </summary>
    /// <param name="desktopIcon">Information about the entry to be removed.</param>
    /// <param name="machineWide">The entry was created machine-wide instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Remove(DesktopIcon desktopIcon, bool machineWide)
    {
        #region Sanity checks
        if (desktopIcon == null) throw new ArgumentNullException(nameof(desktopIcon));
        #endregion

        if (machineWide) return;

        KeyFile.Delete(GetDesktopIconPath(desktopIcon));
    }

    private static string GetDesktopIconPath(DesktopIcon desktopIcon)
        => Paths.Combine(
            // Handles XDG_DESKTOP_DIR from user-dirs.dirs
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolderOption.DoNotVerify),
            $"{desktopIcon.Name}.desktop");
    #endregion

    #region Auto-start
    /// <summary>
    /// Creates a new Desktop Entry that is automatically started when the user logs in.
    /// </summary>
    /// <param name="autoStart">Information about the entry to be created.</param>
    /// <param name="target">The application being integrated.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    /// <param name="machineWide">Create the entry machine-wide instead of just for the current user.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    /// <seealso href="https://specifications.freedesktop.org/autostart-spec/latest/"/>
    public static void Create(AutoStart autoStart, FeedTarget target, IIconStore iconStore, bool machineWide)
    {
        #region Sanity checks
        if (autoStart == null) throw new ArgumentNullException(nameof(autoStart));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        GetDesktopEntry(autoStart, target, iconStore)
           .Save(GetAutoStartPath(autoStart, machineWide));
    }

    /// <summary>
    /// Removes a Desktop Entry that is automatically started when the user logs in.
    /// </summary>
    /// <param name="autoStart">Information about the entry to be removed.</param>
    /// <param name="machineWide">The entry was created machine-wide instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Remove(AutoStart autoStart, bool machineWide)
    {
        #region Sanity checks
        if (autoStart == null) throw new ArgumentNullException(nameof(autoStart));
        #endregion

        KeyFile.Delete(GetAutoStartPath(autoStart, machineWide));
    }

    private static string GetAutoStartPath(AutoStart autoStart, bool machineWide)
        => Paths.Combine(GetConfigDir(machineWide), "autostart", $"{FilePrefix}{FeedUri.Escape(autoStart.Name ?? "")}.desktop");
    #endregion

    #region Desktop entry
    private static KeyFile GetDesktopEntry(CommandAccessPoint accessPoint, FeedTarget target, IIconStore iconStore)
        => GetDesktopEntry(
                target,
                accessPoint.Name,
                accessPoint.Command,
                CommandLine.GetRunArguments(target.Uri, accessPoint.Command),
                fieldCode: null,
                iconStore)
           .Add("StartupWMClass", target.Feed.GetEntryPoint(accessPoint.Command.EmptyAsNull())?.BinaryName);

    /// <summary>
    /// Generates a Desktop Entry for an application.
    /// </summary>
    /// <param name="target">The application to be launched.</param>
    /// <param name="name">The name to display.</param>
    /// <param name="command">The command within <paramref name="target"/> to be launched; can be <c>null</c>.</param>
    /// <param name="arguments">The arguments to pass to Zero Install.</param>
    /// <param name="fieldCode">The field code to replace <see cref="CommandLine.Item"/> in <paramref name="arguments"/> with; <c>null</c> to not replace it.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    internal static KeyFile GetDesktopEntry(FeedTarget target, string? name, string? command, IEnumerable<string> arguments, string? fieldCode, IIconStore iconStore)
    {
        command = command.EmptyAsNull();
        bool needsTerminal = NeedsTerminal(target, command);

        return new KeyFile()
              .Group("Desktop Entry")
              .Add("Type", "Application")
              .Add("Version", "1.0")
              .Add("Name", name ?? target.Feed.Name)
              .Add("Comment", target.Feed.GetBestSummary(CultureInfo.CurrentUICulture, command))
              .Add("Exec", arguments.Prepend(CommandLine.GetLauncher(gui: !needsTerminal)).ToExec(fieldCode))
              .Add("Icon", GetIconPath(target, command, iconStore))
              .Add("Terminal", needsTerminal);
    }

    /// <summary>
    /// Determines whether a command needs to be run in a terminal.
    /// </summary>
    internal static bool NeedsTerminal(FeedTarget target, string? command)
        => target.Feed.GetEntryPoint(command.EmptyAsNull()) is {NeedsTerminal: true};

    /// <summary>
    /// Returns the local path of the best icon for a command in a format supported by freedesktop.org systems; <c>null</c> if there is none.
    /// </summary>
    internal static string? GetIconPath(FeedTarget target, string? command, IIconStore iconStore)
    {
        string[] mimeTypes = [Icon.MimeTypePng, Icon.MimeTypeSvgXml, Icon.MimeTypeSvg];
        Icon? Find(IEnumerable<Icon> icons) => mimeTypes.Select(icons.GetIcon).FirstOrDefault(x => x != null);

        return (target.Feed.GetEntryPoint(command.EmptyAsNull())?.Icons.To(Find) ?? Find(target.Feed.Icons))
           ?.To(iconStore.GetFresh);
    }
    #endregion

    #region Databases
    /// <summary>
    /// Updates databases used by desktop environments to reflect changes to Desktop Entries and MIME types.
    /// </summary>
    /// <param name="machineWide">Update the machine-wide databases instead of those of the current user.</param>
    internal static void UpdateDatabases(bool machineWide)
    {
        string mimeDir = MimeTypes.GetDir(machineWide);
        if (Directory.Exists(Paths.Combine(mimeDir, "packages")))
            TryRun("update-mime-database", [mimeDir]);

        string applicationsDir = GetApplicationsDir(machineWide);
        if (Directory.Exists(applicationsDir))
            TryRun("update-desktop-database", [applicationsDir]);
    }
    #endregion

    /// <summary>
    /// Runs an external tool that may not be installed on all systems.
    /// </summary>
    /// <param name="tool">The name of the tool's executable.</param>
    /// <param name="arguments">The arguments to pass to the tool.</param>
    /// <param name="warnOnFailure">Log a warning if the tool exits with an error; <c>false</c> to only log an info message.</param>
    private static void TryRun(string tool, string[] arguments, bool warnOnFailure = true)
    {
        try
        {
            new ProcessLauncher(tool).RunAndCapture(arguments);
        }
        catch (ExitCodeException ex)
        {
            if (warnOnFailure) Log.Warn($"{tool} failed", ex);
            else Log.Info($"{tool} failed", ex);
        }
        catch (IOException ex)
        {
            Log.Info($"Unable to run {tool}", ex); // Not installed on all systems
        }
    }
}
