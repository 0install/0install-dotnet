// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using PropertyStore.Flags;
using PropertyStore.Structures;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Creates Windows shortcut files (.lnk).
/// </summary>
public static partial class Shortcut
{
    /// <summary>
    /// Creates a new Windows shortcut.
    /// </summary>
    /// <param name="path">The location to place the shortcut at.</param>
    /// <param name="target">The target the shortcut shall point to.</param>
    /// <param name="command">The command within <paramref name="target"/> the shortcut shall point to; can be <c>null</c>.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    private static void Create(string path, FeedTarget target, string? command, IIconStore iconStore)
    {
        if (string.IsNullOrEmpty(command)) command = Command.NameRun;

        var entryPoint = target.Feed.GetEntryPoint(command);
        bool needsTerminal = entryPoint is {NeedsTerminal: true};

        string targetPath = Path.Combine(Locations.InstallBase, needsTerminal ? "0install.exe" : "0install-win.exe");

        string arguments = "run ";
        if (!needsTerminal) arguments += "--no-wait ";
        if (command != Command.NameRun) arguments += $"--command {command.EscapeArgument()} ";
        arguments += target.Uri.ToStringRfc().EscapeArgument();

        var icon = target.Feed.GetBestIcon(Icon.MimeTypeIco, command);

        Create(path, targetPath, arguments,
            iconLocation: icon?.To(iconStore.GetFresh),
            description: target.Feed.GetBestSummary(CultureInfo.CurrentUICulture, command),
            appId: entryPoint?.AppId ?? GuessAppExePath(target.Feed, entryPoint));
    }

    private static string? GuessAppExePath(Feed feed, EntryPoint? entryPoint)
    {
        if (string.IsNullOrEmpty(entryPoint?.BinaryName)) return null;

        string? referenceDigest =
            feed.Implementations
                .OrderByDescending(x => x.Version)
                .FirstOrDefault(x => x.Architecture.RunsOn(Architecture.CurrentSystem))
               ?.ManifestDigest.Best;
        if (referenceDigest == null) return null;

        return Path.Combine(ImplementationStores.GetDirectories().Last(), referenceDigest, $"{entryPoint.BinaryName}.exe");
    }

    /// <summary>
    /// Creates a new Windows shortcut.
    /// </summary>
    /// <param name="path">The location to place the shortcut at.</param>
    /// <param name="targetPath">The target path the shortcut shall point to.</param>
    /// <param name="arguments">Additional arguments to pass to the target; can be <c>null</c>.</param>
    /// <param name="iconLocation">The path of the icon to use for the shortcut; leave <c>null</c> ot get the icon from <paramref name="targetPath"/>.</param>
    /// <param name="description">A short human-readable description; can be <c>null</c>.</param>
    /// <param name="appId">The Application User Model ID; used by Windows to associate shortcuts and pinned taskbar entries with running processes.</param>
    public static void Create(string path, string targetPath, string? arguments = null, string? iconLocation = null, string? description = null, string? appId = null)
    {
        Log.Debug($"Creating Windows shortcut file at '{path}' pointing to: {targetPath} {arguments ?? ""}");

        if (Path.GetDirectoryName(path) is { } dirPath && !Directory.Exists(dirPath))
        {
            Log.Info($"Creating missing directory '{dirPath} for shortcut");
            Directory.CreateDirectory(dirPath);
        }

        var link = ShellLink.Shortcut.CreateShortcut(targetPath, arguments, iconLocation ?? targetPath, iconindex: 0);
        if (!string.IsNullOrEmpty(appId))
        {
            link.ExtraData.PropertyStoreDataBlock = new()
            {
                PropertyStore =
                {
                    new()
                    {
                        FormatID = Guid.Parse("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
                        PropertyStorage = {new IntegerName(5, new(PropertyType.VT_BSTR, ToBstr(appId)))}
                    }
                }
            };
        }
        if (!string.IsNullOrEmpty(description))
            link.StringData.NameString = description;
        link.WriteToFile(path);
    }

    private static byte[] ToBstr(string value)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(value + '\0');
        return ArrayUtils.Concat(BitConverter.GetBytes(bytes.Length), bytes, new byte[] {0, 0});
    }

    private static string GetFolderPath(Environment.SpecialFolder folder)
    {
        try
        {
            string result = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.Create);
            if (string.IsNullOrEmpty(result)) Log.Info($"Got empty path for {folder}, falling back to hardcoded default");
            else return result;
        }
        catch (ArgumentException ex)
        {
            Log.Info($"Error getting path for {folder}, falling back to hardcoded default", ex);
        }

        // Fallback paths in case Environment.GetFolderPath() does not work
        return folder switch
        {
            Environment.SpecialFolder.CommonDesktopDirectory => @"C:\Users\Public\Desktop",
            Environment.SpecialFolder.CommonPrograms => Path.Combine(Locations.SystemConfigDirs, "Microsoft", "Windows", "Start Menu", "Programs"),
            Environment.SpecialFolder.CommonStartup => Path.Combine(Locations.SystemConfigDirs, "Microsoft", "Windows", "Start Menu", "Programs", "Startup"),
            Environment.SpecialFolder.DesktopDirectory => Path.Combine(Locations.HomeDir, "Desktop"),
            Environment.SpecialFolder.Programs => Path.Combine(Locations.UserConfigDir, "Microsoft", "Windows", "Start Menu", "Programs"),
            Environment.SpecialFolder.Startup => Path.Combine(Locations.UserConfigDir, "Microsoft", "Windows", "Start Menu", "Programs", "Startup"),
            Environment.SpecialFolder.SendTo => Path.Combine(Locations.UserConfigDir, "Microsoft", "SendTo"),
            _ => Locations.HomeDir
        };
    }
}
