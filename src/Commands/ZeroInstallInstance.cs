// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using NanoByte.Common.Info;
using NanoByte.Common.Native;
using NanoByte.Common.Net;
using ZeroInstall.Services;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Trust;

#if NETFRAMEWORK
using System.Diagnostics;
#endif

namespace ZeroInstall.Commands;

/// <summary>
/// Provides information about the currently running instance of Zero Install.
/// </summary>
public static class ZeroInstallInstance
{
    /// <summary>
    /// The current version of Zero Install.
    /// </summary>
    public static ImplementationVersion Version { get; }
        = GetZeroInstallForWindowsVersion()
       ?? new(AppInfo.CurrentLibrary.Version ?? "1.0.0-pre");

    private static ImplementationVersion? GetZeroInstallForWindowsVersion()
    {
#if NETFRAMEWORK
        string path = Path.Combine(Locations.InstallBase, "0install-win.exe");
        if (!File.Exists(path)) return null;

        try
        {
            return new(FileVersionInfo.GetVersionInfo(path).ProductVersion.GetLeftPartAtFirstOccurrence('+'));
        }
        catch
#endif
        {
            return null;
        }
    }

    /// <summary>
    /// Indicates whether the current Zero Install instance is deployed to a fixed location.
    /// </summary>
    public static bool IsDeployed
        => !Locations.IsPortable
        && WindowsUtils.IsWindows
        && (RegistryUtils.GetSoftwareString(RegKeyName, InstallLocation, machineWide: false) == Locations.InstallBase
         || RegistryUtils.GetSoftwareString(RegKeyName, InstallLocation, machineWide: true) == Locations.InstallBase);

    /// <summary>
    /// Indicates whether Zero Install is running from a machine-wide location.
    /// </summary>
    public static bool IsMachineWide
    {
        get
        {
            if (Locations.IsPortable) return false;

            if (WindowsUtils.IsWindows)
            {
                string? path = RegistryUtils.GetSoftwareString(RegKeyName, InstallLocation, machineWide: true);
                if (!string.IsNullOrEmpty(path)) return path == Locations.InstallBase;
            }

            return !Locations.InstallBase.StartsWith(Locations.HomeDir);
        }
    }

    /// <summary>
    /// Indicates whether the current Zero Install instance is in library mode.
    /// </summary>
    public static bool IsLibraryMode
        => !Locations.IsPortable
        && WindowsUtils.IsWindows
        && IsDeployed
        && RegistryUtils.GetSoftwareString(RegKeyName, LibraryMode, IsMachineWide) == "1";

    /// <summary>
    /// Indicates whether the current Zero Install instance is integrated into the desktop environment.
    /// </summary>
    public static bool IsIntegrated
        => !Locations.IsPortable
        && WindowsUtils.IsWindows
        && IsDeployed
        && !IsLibraryMode;

    /// <summary>
    /// Registers a Zero Install instance in the Windows registry if possible.
    /// </summary>
    /// <param name="path">The deployment directory of the instance of Zero Install.</param>
    /// <param name="machineWide"><c>true</c> if <paramref name="path"/> is a machine-wide location; <c>false</c> if it is a user-specific location.</param>
    /// <param name="libraryMode">Indicates the instance was deployed in library mode.</param>
    [SupportedOSPlatform("windows")]
    public static void RegisterLocation(string path, bool machineWide, bool libraryMode)
    {
        RegistryUtils.SetSoftwareString(RegKeyName, InstallLocation, path, machineWide);
        RegistryUtils.SetSoftwareString(RegKeyName, LibraryMode, libraryMode ? "1" : "0", machineWide);
    }

    /// <summary>
    /// Unregisters a Zero Install instance from the Windows registry if possible.
    /// </summary>
    /// <param name="machineWide"><c>true</c> if a machine-wide registration should be removed; <c>false</c> if a user-specific registration should be removed.</param>
    [SupportedOSPlatform("windows")]
    public static void UnregisterLocation(bool machineWide)
    {
        RegistryUtils.DeleteSoftwareValue(RegKeyName, InstallLocation, machineWide);
        RegistryUtils.DeleteSoftwareValue(RegKeyName, LibraryMode, machineWide);
    }

    /// <summary>
    /// Tries to find another instance of Zero Install deployed on this system.
    /// </summary>
    /// <param name="needsMachineWide"><c>true</c> if a machine-wide install location is required; <c>false</c> if a user-specific location will also do.</param>
    /// <returns>The deployment directory of another instance of Zero Install; <c>null</c> if none was found.</returns>
    public static string? FindOther(bool needsMachineWide = false)
    {
        if (!WindowsUtils.IsWindows) return null;

        string? installLocation = RegistryUtils.GetSoftwareString(RegKeyName, InstallLocation);
        if (string.IsNullOrEmpty(installLocation)) return null;
        if (installLocation == Locations.InstallBase) return null;
        if (needsMachineWide && installLocation.StartsWith(Locations.HomeDir)) return null;
        if (!File.Exists(Path.Combine(installLocation, "0install.exe"))) return null;
        return installLocation;
    }

    private const string RegKeyName = "Zero Install";
    private const string InstallLocation = "InstallLocation";
    private const string LibraryMode = "LibraryMode";

    /// <summary>
    /// Silently checks if an update for Zero Install is available.
    /// </summary>
    /// <returns>The version number of the newest available update; <c>null</c> if no update is available.</returns>
    public static ImplementationVersion? SilentUpdateCheck()
    {
        if (!IsDeployed || !NetUtils.IsInternetConnected) return null;

        using var handler = new SilentTaskHandler();
        var services = new ServiceProvider(handler) {FeedManager = {Refresh = true}};
        if (services.Config is {NetworkUse: NetworkLevel.Offline} or {SelfUpdateUri: null}) return null;

        try
        {
            var selections = services.Solver.Solve(services.Config.SelfUpdateUri);
            var newVersion = selections.MainImplementation.Version;
            return (newVersion > Version) ? newVersion : null;
        }
        #region Error handling
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (WebException ex)
        {
            Log.Debug(ex.Message, ex);
            return null;
        }
        catch (Exception ex) when (ex is UriFormatException or IOException or UnauthorizedAccessException or SignatureException or SolverException or InvalidDataException)
        {
            Log.Warn("Problem with silent self-update check", ex);
            return null;
        }
        #endregion
    }
}
