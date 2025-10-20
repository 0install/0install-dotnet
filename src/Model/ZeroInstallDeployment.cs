// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.Model;

/// <summary>
/// Registers and discovers Zero Install deployments on this system.
/// </summary>
public static class ZeroInstallDeployment
{
    private const string
        RegKeyName = "Zero Install",
        InstallLocation = "InstallLocation",
        LibraryMode = "LibraryMode";

    /// <summary>
    /// Tries to find a deployment of Zero Install.
    /// </summary>
    /// <param name="machineWide"><c>true</c> to look for machine-wide deployments; <c>false</c> to look for user-specific deployments.</param>
    /// <returns>The directory path of a deployment of Zero Install; <c>null</c> if none was found.</returns>
    public static string? GetPath(bool machineWide)
    {
        if (WindowsUtils.IsWindows
         && RegistryUtils.GetSoftwareString(RegKeyName, InstallLocation, machineWide) is {Length: > 0} path)
        {
            try
            {
                if (File.Exists(Path.Combine(path, "0install.exe"))) return path;
            }
            #region Error handling
            catch (ArgumentException ex)
            {
                Log.Warn($"Invalid Zero Install path found in registry: {path}", ex);
            }
            #endregion
        }

        return null;
    }

    /// <summary>
    /// Indicates whether a deployment of Zero Install was made in library mode.
    /// </summary>
    /// <param name="machineWide"><c>true</c> to look for machine-wide deployments; <c>false</c> to look for user-specific deployments.</param>
    public static bool IsLibraryMode(bool machineWide)
        => WindowsUtils.IsWindows
        && RegistryUtils.GetSoftwareString(RegKeyName, LibraryMode, machineWide) == "1";

    /// <summary>
    /// Tries to find a deployment of Zero Install that is not the currently running one.
    /// </summary>
    /// <param name="needsMachineWide"><c>true</c> if a machine-wide deployment is required; <c>false</c> if a user-specific deployment will also do.</param>
    /// <returns>The directory path of a deployment of Zero Install; <c>null</c> if none was found.</returns>
    public static string? FindOther(bool needsMachineWide = false)
    {
        static string? Get(bool machineWide)
            => GetPath(machineWide) is {} path
            && !FileUtils.PathEquals(path, Locations.InstallBase)
                ? path
                : null;

        return needsMachineWide
            ? Get(machineWide: true)
            : Get(machineWide: false) ?? Get(machineWide: true);
    }

    /// <summary>
    /// Registers a Zero Install deployment in the Windows registry if possible.
    /// </summary>
    /// <param name="path">The directory path of the deployment of Zero Install.</param>
    /// <param name="machineWide"><c>true</c> if <paramref name="path"/> is a machine-wide location; <c>false</c> if it is a user-specific location.</param>
    /// <param name="libraryMode">Indicates whether Zero Install was deployed as a library for use by other applications.</param>
    public static void Register(string path, bool machineWide, bool libraryMode)
    {
        if (!WindowsUtils.IsWindows) return;

        RegistryUtils.SetSoftwareString(RegKeyName, InstallLocation, path, machineWide);
        RegistryUtils.SetSoftwareString(RegKeyName, LibraryMode, libraryMode ? "1" : "0", machineWide);
    }

    /// <summary>
    /// Unregisters a Zero Install deployment from the Windows registry if possible.
    /// </summary>
    /// <param name="machineWide"><c>true</c> if a machine-wide registration should be removed; <c>false</c> if a user-specific registration should be removed.</param>
    public static void Unregister(bool machineWide)
    {
        if (!WindowsUtils.IsWindows) return;

        RegistryUtils.DeleteSoftwareValue(RegKeyName, InstallLocation, machineWide);
        RegistryUtils.DeleteSoftwareValue(RegKeyName, LibraryMode, machineWide);
    }
}
