// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Contains control logic for applying <see cref="BrowserNativeMessaging"/> on Linux and similar systems.
/// </summary>
public static class BrowserNativeMessagingHost
{
    /// <summary>
    /// Registers a browser native messaging host in the current system.
    /// </summary>
    /// <param name="target">The application being integrated.</param>
    /// <param name="capability">The native messaging host to register.</param>
    /// <param name="machineWide">Register the native messaging host machine-wide instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Register(FeedTarget target, BrowserNativeMessaging capability, bool machineWide)
    {
        #region Sanity checks
        if (capability == null) throw new ArgumentNullException(nameof(capability));
        #endregion

        foreach (string browser in capability.Browsers)
        {
            if (TryGetManifestPath(browser, capability.Name, machineWide) is not {} manifestPath) continue;

            if (File.Exists(manifestPath) && !IsOwn(manifestPath, capability, machineWide))
            {
                string? existingScript = TryGetScriptPath(manifestPath);
                if (existingScript == null || !existingScript.StartsWithOrdinal(GetScriptDir(machineWide) + "/"))
                {
                    Log.Warn(string.Format(Resources.AliasFileNotOwned, manifestPath));
                    continue;
                }
                if (File.Exists(existingScript))
                {
                    Log.Warn($"Native messaging host '{capability.Name}' for {browser} skipped, because '{manifestPath}' is already used by another browser's host");
                    continue;
                }
            }

            // Native messaging manifests cannot specify command-line arguments
            string scriptPath = GetScriptPath(browser, capability.Name, machineWide);
            Write(scriptPath, GetScript(target.Uri, capability.Command), executable: true);

            var manifest = BrowserNativeMessagingManifest.For(
                browser,
                capability.Name,
                target.Feed.GetBestSummary(CultureInfo.CurrentUICulture, capability.Command) ?? "",
                scriptPath,
                capability.BrowserExtensions);
            Write(manifestPath, manifest.ToJsonString());
        }
    }

    /// <summary>
    /// Unregisters a browser native messaging host in the current system.
    /// </summary>
    /// <param name="capability">The native messaging host to remove.</param>
    /// <param name="machineWide">The native messaging host was registered machine-wide instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Unregister(BrowserNativeMessaging capability, bool machineWide)
    {
        #region Sanity checks
        if (capability == null) throw new ArgumentNullException(nameof(capability));
        #endregion

        foreach (string browser in capability.Browsers)
        {
            if (TryGetManifestPath(browser, capability.Name, machineWide) is not {} manifestPath) continue;

            if (File.Exists(manifestPath) && IsOwn(manifestPath, capability, machineWide)) File.Delete(manifestPath);

            string scriptPath = GetScriptPath(browser, capability.Name, machineWide);
            if (File.Exists(scriptPath)) File.Delete(scriptPath);
        }
    }

    private static string GetScript(FeedUri uri, string? command)
        => $"""
            #!/bin/sh
            exec {CommandLine.GetRunArguments(uri, command).Prepend(CommandLine.GetLauncher(gui: false)).JoinEscapeArgumentsPosix()} "$@"

            """.ToUnixNewLine();

    private static void Write(string path, string content, bool executable = false)
    {
        using var atomic = new AtomicWrite(path);
        File.WriteAllText(atomic.WritePath, content);
        if (executable) FileUtils.SetExecutable(atomic.WritePath, true);
        atomic.Commit();
    }

    /// <summary>
    /// Determines whether a manifest was generated for a specific capability.
    /// </summary>
    /// <remarks>
    /// Some browsers share a machine-wide directory and the file name of the manifest must match the host name.
    /// Therefore, the script path, which contains the browser name, is used to determine which browser the manifest was generated for.
    /// </remarks>
    private static bool IsOwn(string manifestPath, BrowserNativeMessaging capability, bool machineWide)
        => TryGetScriptPath(manifestPath) is {} scriptPath
        && capability.Browsers.Any(x => GetScriptPath(x, capability.Name, machineWide) == scriptPath);

    private static string? TryGetScriptPath(string manifestPath)
    {
        try
        {
            return JsonStorage.LoadJson<BrowserNativeMessagingManifest>(manifestPath).Path;
        }
        #region Error handling
        catch (InvalidDataException ex)
        {
            Log.Debug($"Unable to parse {manifestPath}", ex);
            return null;
        }
        #endregion
    }

    private static string GetScriptDir(bool machineWide)
        => IntegrationManager.GetDir(machineWide, "browser-native-messaging");

    private static string GetScriptPath(string browser, string hostName, bool machineWide)
        => Paths.Combine(GetScriptDir(machineWide), browser, hostName);

    private static string? TryGetManifestPath(string browser, string hostName, bool machineWide)
        => (machineWide ? TryGetMachineDir(browser) : TryGetUserDir(browser))
          ?.To(x => Paths.Combine(x, $"{hostName}.json"));

    private static string? TryGetUserDir(string browser)
        => browser switch
        {
            KnownBrowsers.Firefox => Paths.Combine(Locations.HomeDir, ".mozilla", "native-messaging-hosts"),
            KnownBrowsers.Chrome => ChromiumDir("google-chrome"),
            KnownBrowsers.Chromium => ChromiumDir("chromium"),
            KnownBrowsers.Edge => ChromiumDir("microsoft-edge"),
            KnownBrowsers.Opera => ChromiumDir("opera"),
            KnownBrowsers.Brave => ChromiumDir("BraveSoftware/Brave-Browser"),
            KnownBrowsers.Vivaldi => ChromiumDir("vivaldi"),
            _ => null
        };

    private static string ChromiumDir(string name)
        => Paths.Combine(Locations.UserConfigDir, name, "NativeMessagingHosts");

    private static string? TryGetMachineDir(string browser)
        => browser switch
        {
            KnownBrowsers.Firefox => "/usr/lib/mozilla/native-messaging-hosts",
            KnownBrowsers.Chrome => "/etc/opt/chrome/native-messaging-hosts",
            KnownBrowsers.Edge => "/etc/opt/edge/native-messaging-hosts",
            KnownBrowsers.Chromium or KnownBrowsers.Opera or KnownBrowsers.Brave or KnownBrowsers.Vivaldi => "/etc/chromium/native-messaging-hosts",
            _ => null
        };
}
