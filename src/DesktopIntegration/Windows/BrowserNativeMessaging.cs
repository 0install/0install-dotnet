// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using Microsoft.Win32;
using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Contains control logic for applying <see cref="Model.Capabilities.BrowserNativeMessaging"/> and <see cref="AccessPoints.BrowserNativeMessaging"/> on Windows systems.
/// </summary>
[SupportedOSPlatform("windows")]
public static class BrowserNativeMessaging
{
    public static void Register(FeedTarget target, Model.Capabilities.BrowserNativeMessaging nativeMessaging, IIconStore iconStore, bool machineWide)
    {
        var manifest = new BrowserNativeMessagingManifest(
            Name: nativeMessaging.Name,
            Description: target.Feed.GetBestSummary(CultureInfo.CurrentUICulture, nativeMessaging.Command) ?? "",
            Path: BuildStub(),
            AllowedOrigins: nativeMessaging.BrowserExtensions.Select(x => x.ID));
        string manifestPath = GetManifestPath(nativeMessaging, machineWide);
        using (var atomic = new AtomicWrite(manifestPath))
            File.WriteAllText(atomic.WritePath, manifest.ToJsonString());

        using var registryKey = OpenRegistryKey(nativeMessaging.Browser, machineWide);
        using var subKey = registryKey.CreateSubKeyChecked(nativeMessaging.Name);
        subKey.SetValue("", manifestPath);

        string BuildStub()
        {
            try
            {
                return new StubBuilder(iconStore).GetRunCommandLine(target, nativeMessaging.Command, machineWide).Single();
            }
            catch (InvalidOperationException)
            {
                throw new IOException("Failed to generate stub EXE.");
            }
        }
    }

    public static void Unregister(Model.Capabilities.BrowserNativeMessaging nativeMessaging, bool machineWide)
    {
        using var registryKey = OpenRegistryKey(nativeMessaging.Browser, machineWide);
        registryKey.TryDeleteSubKey(nativeMessaging.Name);

        File.Delete(GetManifestPath(nativeMessaging, machineWide));
    }

    private static string GetManifestPath(Model.Capabilities.BrowserNativeMessaging nativeMessaging, bool machineWide)
        => Path.Combine(
            IntegrationManager.GetDir(machineWide, "browser-native-messaging"),
            $"{nativeMessaging.Name}.json");

    private static RegistryKey OpenRegistryKey(Browser browser, bool machineWide)
    {
        var hive = machineWide ? Registry.LocalMachine : Registry.CurrentUser;
        string infix = browser switch
        {
            Browser.Chrome or Browser.Chromium => @"Google\Chrome",
            Browser.Edge => @"Microsoft\Edge",
            Browser.Firefox => "Mozilla",
            Browser.Opera => "Opera Software",
            Browser.Brave => "BraveSoftware/Brave-Browser",
            Browser.Vivaldi => "Vivaldi",
            _ => throw new NotSupportedException("Unsupported browser: " + browser)
        };
        return hive.OpenSubKeyChecked($@"Software\{infix}\NativeMessagingHosts", writable: true);
    }
}
