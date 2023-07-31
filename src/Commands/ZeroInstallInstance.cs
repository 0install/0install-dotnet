// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Info;
using NanoByte.Common.Native;
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
    {
        get
        {
            if (Locations.IsPortable) return false;

            if (WindowsUtils.IsWindows)
            {
                return FileUtils.PathEquals(ZeroInstallDeployment.GetPath(machineWide: false), Locations.InstallBase)
                    || FileUtils.PathEquals(ZeroInstallDeployment.GetPath(machineWide: true), Locations.InstallBase);
            }
            else if (UnixUtils.IsUnix)
            {
                return !Locations.InstallBase.Contains("/tmp")
                    && !Locations.InstallBase.Contains("/.cache");
            }
            else return true;
        }
    }

    /// <summary>
    /// Indicates whether Zero Install is running from a machine-wide location.
    /// </summary>
    public static bool IsMachineWide
    {
        get
        {
            if (Locations.IsPortable) return false;
            if (ZeroInstallDeployment.GetPath(machineWide: true) is {} path)
                return FileUtils.PathEquals(path, Locations.InstallBase);
            return !Locations.InstallBase.StartsWith(Locations.HomeDir);
        }
    }

    /// <summary>
    /// Indicates whether the current Zero Install instance is in library mode.
    /// </summary>
    public static bool IsLibraryMode
        => !Locations.IsPortable
        && IsDeployed
        && ZeroInstallDeployment.IsLibraryMode(IsMachineWide);

    /// <summary>
    /// Indicates whether the current Zero Install instance is integrated into the desktop environment.
    /// </summary>
    public static bool IsIntegrated
        => !Locations.IsPortable
        && WindowsUtils.IsWindows
        && IsDeployed
        && !IsLibraryMode;

    /// <summary>
    /// Silently checks if an update for Zero Install is available.
    /// </summary>
    /// <returns>The version number of the newest available update; <c>null</c> if no update is available.</returns>
    public static ImplementationVersion? SilentUpdateCheck()
    {
        if (!IsDeployed) return null;

        using var handler = new SilentTaskHandler();
        var services = new ServiceProvider(handler) {FeedManager = {Refresh = true}};
        if (services.Config is {EffectiveNetworkUse: NetworkLevel.Offline} or {SelfUpdateUri: null}) return null;

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
            Log.Debug(ex);
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
