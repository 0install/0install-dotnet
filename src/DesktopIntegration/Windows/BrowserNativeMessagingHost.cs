// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using Microsoft.Win32;
using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Contains control logic for applying <see cref="BrowserNativeMessaging"/> on Windows systems.
/// </summary>
[SupportedOSPlatform("windows")]
public static class BrowserNativeMessagingHost
{
    public static void Register(FeedTarget target, BrowserNativeMessaging capability, IIconStore iconStore, bool machineWide)
    {
        string stubPath;
        try
        {
            stubPath = new StubBuilder(iconStore).GetRunCommandLine(target, capability.Command, machineWide, needsTerminal: true).Single();
        }
        catch (InvalidOperationException)
        {
            throw new IOException("Failed to generate stub EXE.");
        }

        string description = target.Feed.GetBestSummary(CultureInfo.CurrentUICulture, capability.Command) ?? "";

        foreach (string browser in capability.Browsers)
            Register(browser, capability.Name, description, stubPath, capability.BrowserExtensions, machineWide);
    }

    private static void Register(string browser, string hostName, string description, string stubPath, IEnumerable<BrowserExtension> extensions, bool machineWide)
    {
        using var registryKey = TryOpenRegistryKey(browser, machineWide);
        if (registryKey == null) return;

        var manifest = new BrowserNativeMessagingManifest(hostName, description, stubPath);
        if (browser == KnownBrowsers.Firefox)
            manifest.AllowedExtensions = extensions.Select(x => x.ID);
        else
            manifest.AllowedOrigins = extensions.Select(x => $"chrome-extension://{x.ID}/");

        string manifestPath = GetManifestPath(browser, hostName, machineWide);
        using (var atomic = new AtomicWrite(manifestPath))
        {
            File.WriteAllText(atomic.WritePath, manifest.ToJsonString());
            atomic.Commit();
        }

        using var subKey = registryKey.CreateSubKeyChecked(hostName);
        subKey.SetValue("", manifestPath);
    }

    public static void Unregister(BrowserNativeMessaging capability, bool machineWide)
    {
        foreach (string browser in capability.Browsers)
            Unregister(browser, capability.Name, machineWide);
    }

    private static void Unregister(string browser, string hostName, bool machineWide)
    {
        using (var registryKey = TryOpenRegistryKey(browser, machineWide))
            registryKey?.TryDeleteSubKey(hostName);

        File.Delete(GetManifestPath(browser, hostName, machineWide));
    }

    private static string GetManifestPath(string browser, string hostName, bool machineWide)
        => Path.Combine(
            IntegrationManager.GetDir(machineWide, "browser-native-messaging", browser),
            $"{hostName}.json");

    private static RegistryKey? TryOpenRegistryKey(string browser, bool machineWide)
    {
        var hive = machineWide ? Registry.LocalMachine : Registry.CurrentUser;

        string? infix = browser switch
        {
            KnownBrowsers.Chrome or KnownBrowsers.Chromium => @"Google\Chrome",
            KnownBrowsers.Edge => @"Microsoft\Edge",
            KnownBrowsers.Firefox => "Mozilla",
            KnownBrowsers.Opera => "Opera Software",
            KnownBrowsers.Brave => @"BraveSoftware\Brave-Browser",
            KnownBrowsers.Vivaldi => "Vivaldi",
            _ => null
        };

        return infix == null
            ? null
            : hive.CreateSubKeyChecked($@"Software\{infix}\NativeMessagingHosts");
    }
}
