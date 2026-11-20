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

    private const string
        AppName = "0install.net",
        InstallLocationFile = "deployment_location",
        LibraryModeFile = "library_mode";

    /// <summary>
    /// Tries to find a deployment of Zero Install.
    /// </summary>
    /// <param name="machineWide"><c>true</c> to look for machine-wide deployments; <c>false</c> to look for user-specific deployments.</param>
    /// <returns>The directory path of a deployment of Zero Install; <c>null</c> if none was found.</returns>
    public static string? GetPath(bool machineWide)
    {
        if (ReadInstallLocation(machineWide) is {Length: > 0} path)
        {
            try
            {
                if (ContainsExecutable(path)) return path;
            }
            #region Error handling
            catch (IOException ex)
            {
                Log.Warn($"Invalid Zero Install deployment path: {path}", ex);
            }
            #endregion
        }

        return null;
    }

    private static string? ReadInstallLocation(bool machineWide)
    {
        if (WindowsUtils.IsWindows)
            return RegistryUtils.GetSoftwareString(RegKeyName, InstallLocation, machineWide);

        try
        {
            string path = GetConfigFilePath(InstallLocationFile, machineWide);
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Debug($"Unable to read Zero Install deployment location (machineWide={machineWide})", ex);
            return null;
        }
        #endregion
    }

    private static bool ContainsExecutable(string path)
        => WindowsUtils.IsWindows
            ? File.Exists(Paths.Combine(path, "0install.exe"))
            : File.Exists(Paths.Combine(path, "0install"));

    /// <summary>
    /// Indicates whether a deployment of Zero Install was made in library mode.
    /// </summary>
    /// <param name="machineWide"><c>true</c> to look for machine-wide deployments; <c>false</c> to look for user-specific deployments.</param>
    public static bool IsLibraryMode(bool machineWide)
    {
        if (WindowsUtils.IsWindows)
            return RegistryUtils.GetSoftwareString(RegKeyName, LibraryMode, machineWide) == "1";

        try
        {
            return File.Exists(GetConfigFilePath(LibraryModeFile, machineWide));
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Debug($"Unable to check Zero Install library mode (machineWide={machineWide})", ex);
            return false;
        }
        #endregion
    }

    /// <summary>
    /// Registers a Zero Install deployment.
    /// </summary>
    /// <param name="path">The directory path of the deployment of Zero Install.</param>
    /// <param name="machineWide"><c>true</c> if <paramref name="path"/> is a machine-wide location; <c>false</c> if it is a user-specific location.</param>
    /// <param name="libraryMode">Indicates whether Zero Install was deployed as a library for use by other applications.</param>
    /// <exception cref="IOException">Failed to write the config files.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the registry or config files is not permitted.</exception>
    public static void Register(string path, bool machineWide, bool libraryMode)
    {
        if (WindowsUtils.IsWindows)
        {
            RegistryUtils.SetSoftwareString(RegKeyName, InstallLocation, path, machineWide);
            RegistryUtils.SetSoftwareString(RegKeyName, LibraryMode, libraryMode ? "1" : "0", machineWide);
        }
        else
        {
            File.WriteAllText(GetSaveConfigFilePath(InstallLocationFile, machineWide), path);

            string libraryModePath = GetSaveConfigFilePath(LibraryModeFile, machineWide);
            if (libraryMode) File.WriteAllText(libraryModePath, "");
            else if (File.Exists(libraryModePath)) File.Delete(libraryModePath);
        }
    }

    /// <summary>
    /// Unregisters a Zero Install deployment.
    /// </summary>
    /// <param name="machineWide"><c>true</c> if a machine-wide registration should be removed; <c>false</c> if a user-specific registration should be removed.</param>
    /// <exception cref="IOException">Failed to delete the config files.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the registry or config files is not permitted.</exception>
    public static void Unregister(bool machineWide)
    {
        if (WindowsUtils.IsWindows)
        {
            RegistryUtils.DeleteSoftwareValue(RegKeyName, InstallLocation, machineWide);
            RegistryUtils.DeleteSoftwareValue(RegKeyName, LibraryMode, machineWide);
        }
        else
        {
            foreach (string fileName in new[] {InstallLocationFile, LibraryModeFile})
            {
                string path = GetConfigFilePath(fileName, machineWide);
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    private static string GetConfigFilePath(string fileName, bool machineWide)
        => Path.Combine(
            machineWide ? Locations.SystemConfigDirs.Split(Path.PathSeparator).Last() : Locations.UserConfigDir,
            AppName,
            fileName);

    private static string GetSaveConfigFilePath(string fileName, bool machineWide)
    {
        string path = GetConfigFilePath(fileName, machineWide);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }
}
