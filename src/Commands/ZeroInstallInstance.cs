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
       ?? ImplementationVersion.FromSemVer(AppInfo.Load(typeof(ZeroInstallInstance).Assembly).Version ?? "1.0.0-pre");

    private static ImplementationVersion? GetZeroInstallForWindowsVersion()
    {
#if NETFRAMEWORK
        string path = Paths.Combine(Locations.InstallBase, "0install-win.exe");
        if (!File.Exists(path)) return null;

        try
        {
            return ImplementationVersion.FromSemVer(FileVersionInfo.GetVersionInfo(path).ProductVersion);
        }
        catch
#endif
        {
            return null;
        }
    }

    /// <summary>
    /// The root directory of the current Zero Install deployment.
    /// </summary>
    /// <remarks>
    /// Use this to get the directory containing the Zero Install deployment as a whole (e.g., in the form of an app bundle).
    /// Use <see cref="Locations.InstallBase"/> instead to get the directory containing Zero Install's binaries.
    /// </remarks>
    public static string DeploymentRoot
        => UnixUtils.IsMacOSX
        && new DirectoryInfo(Locations.InstallBase) is { Name: "MacOS", Parent: { Name: "Contents", Parent: {} root } contentsDir }
        && File.Exists(Path.Combine(contentsDir.FullName, "Info.plist"))
            ? root.FullName
            : Locations.InstallBase;

    /// <summary>
    /// Indicates whether the current Zero Install instance is deployed to a registered location.
    /// </summary>
    public static bool IsDeployed
        => !Locations.IsPortable
        && (FileUtils.PathEquals(ZeroInstallDeployment.GetPath(machineWide: false), DeploymentRoot)
         || FileUtils.PathEquals(ZeroInstallDeployment.GetPath(machineWide: true), DeploymentRoot));

    /// <summary>
    /// Indicates whether Zero Install is running from a machine-wide location.
    /// </summary>
    public static bool IsMachineWide
    {
        get
        {
            if (Locations.IsPortable) return false;
            if (ZeroInstallDeployment.GetPath(machineWide: true) is {} path)
                return FileUtils.PathEquals(path, DeploymentRoot);
            return !DeploymentRoot.StartsWith(Locations.HomeDir);
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

    /// <summary>
    /// Tries to find a deployment of Zero Install that is not the currently running instance.
    /// </summary>
    /// <param name="needsMachineWide"><c>true</c> if a machine-wide deployment is required; <c>false</c> if a user-specific deployment will also do.</param>
    /// <returns>The directory path of a deployment of Zero Install; <c>null</c> if none was found.</returns>
    public static string? FindOtherDeployment(bool needsMachineWide = false)
    {
        static string? Get(bool machineWide)
            => ZeroInstallDeployment.GetPath(machineWide) is {} path
            && !FileUtils.PathEquals(path, DeploymentRoot)
                ? path
                : null;

        return needsMachineWide
            ? Get(machineWide: true)
            : Get(machineWide: false) ?? Get(machineWide: true);
    }
}
