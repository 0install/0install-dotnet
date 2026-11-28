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
    /// Tries to find a deployed Zero Install executable, falling back to well-known locations if no registered deployment is found.
    /// </summary>
    /// <param name="gui">Whether to return a GUI executable instead of a command-line executable.</param>
    /// <param name="needsMachineWide"><c>true</c> if a machine-wide deployment is required; <c>false</c> if a user-specific deployment will also do.</param>
    /// <returns>The path of a deployed Zero Install executable; <c>null</c> if none was found.</returns>
    /// <remarks>Searches the same locations in the same order as <see cref="Find"/>.</remarks>
    public static string? GetExecutable(bool gui = false, bool needsMachineWide = false)
        => GetCandidates(needsMachineWide)
          .Select(path => TryFindExecutable(path, gui))
          .FirstOrDefault(executable => executable != null);

    /// <summary>
    /// Tries to find a Zero Install executable within a given deployment directory.
    /// </summary>
    /// <param name="deploymentPath">The directory path of a deployment of Zero Install.</param>
    /// <param name="gui">Whether to look for a GUI executable instead of a command-line executable.</param>
    /// <returns>The path of the executable; <c>null</c> if none was found.</returns>
    public static string? FindExecutable(string deploymentPath, bool gui = false)
        => GetExecutableCandidates(deploymentPath, gui).FirstOrDefault(File.Exists);

    /// <summary>
    /// Lists the paths where a Zero Install executable may be located within a deployment, in order of preference.
    /// </summary>
    private static IEnumerable<string> GetExecutableCandidates(string deploymentPath, bool gui)
    {
        if (WindowsUtils.IsWindows)
            yield return Paths.Combine(deploymentPath, gui ? "0install-win.exe" : "0install.exe");
        else
            yield return Paths.Combine(GetBinaryDir(deploymentPath), gui ? "0install-gui" : "0install");
    }

    /// <summary>
    /// Indicates whether a directory is a macOS app bundle, i.e., has a <c>Contents/Info.plist</c> file.
    /// </summary>
    /// <param name="path">The directory to check.</param>
    /// <remarks>This check is platform-independent. Callers decide whether app bundles are relevant on the current platform.</remarks>
    public static bool IsAppBundle(string path)
        => File.Exists(Paths.Combine(path, "Contents", "Info.plist"));

    /// <summary>
    /// Indicates whether a deployment of Zero Install uses the app bundle layout on the current platform.
    /// </summary>
    /// <param name="deploymentPath">The directory path of a deployment of Zero Install.</param>
    /// <remarks>Unlike <see cref="IsAppBundle"/>, this only returns <c>true</c> on macOS.</remarks>
    public static bool UsesAppBundleLayout(string deploymentPath)
        => UnixUtils.IsMacOSX && IsAppBundle(deploymentPath);

    /// <summary>
    /// Gets the directory containing the binaries of a deployment of Zero Install.
    /// </summary>
    /// <param name="deploymentPath">The directory path of a deployment of Zero Install.</param>
    public static string GetBinaryDir(string deploymentPath)
        => GetBinaryDir(deploymentPath, UsesAppBundleLayout(deploymentPath));

    /// <summary>
    /// Gets the directory containing the binaries of a deployment of Zero Install with a known layout.
    /// </summary>
    /// <param name="deploymentPath">The directory path of a deployment of Zero Install.</param>
    /// <param name="appBundle">Whether the deployment uses the app bundle layout.</param>
    /// <remarks>Use this when the layout cannot be detected from the file system, e.g., while files are being staged.</remarks>
    public static string GetBinaryDir(string deploymentPath, bool appBundle)
        => appBundle
            ? Paths.Combine(deploymentPath, "Contents", "MacOS")
            : deploymentPath;

    /// <summary>
    /// Indicates whether a deployment of Zero Install is marked as portable.
    /// </summary>
    public static bool IsPortable(string deploymentPath)
        => File.Exists(GetPortableFlagPath(deploymentPath, UsesAppBundleLayout(deploymentPath)));

    /// <summary>
    /// Changes whether a deployment of Zero Install is marked as portable.
    /// </summary>
    /// <param name="deploymentPath">The directory path of a deployment of Zero Install.</param>
    /// <param name="portable"><c>true</c> to mark it as portable, <c>false</c> to remove the mark.</param>
    /// <exception cref="IOException">Failed to create or delete the portable marker.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the portable marker is not permitted.</exception>
    public static void SetPortable(string deploymentPath, bool portable)
        => SetPortable(deploymentPath, portable, UsesAppBundleLayout(deploymentPath));

    /// <summary>
    /// Changes whether a deployment of Zero Install with a known layout is marked as portable.
    /// </summary>
    /// <param name="deploymentPath">The directory path of a deployment of Zero Install.</param>
    /// <param name="portable"><c>true</c> to mark it as portable, <c>false</c> to remove the mark.</param>
    /// <param name="appBundle">Whether the deployment uses the app bundle layout.</param>
    /// <remarks>Use this when the layout cannot be detected from the file system, e.g., while files are being staged.</remarks>
    /// <exception cref="IOException">Failed to create or delete the portable marker.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the portable marker is not permitted.</exception>
    public static void SetPortable(string deploymentPath, bool portable, bool appBundle)
    {
        string flagPath = GetPortableFlagPath(deploymentPath, appBundle);
        if (portable) FileUtils.Touch(flagPath);
        else File.Delete(flagPath);
    }

    private static string GetPortableFlagPath(string deploymentPath, bool appBundle)
        => Paths.Combine(GetBinaryDir(deploymentPath, appBundle), Locations.PortableFlagName);

    /// <summary>
    /// Tries to find a registered deployment of Zero Install.
    /// </summary>
    /// <param name="machineWide"><c>true</c> to look for machine-wide deployments; <c>false</c> to look for user-specific deployments.</param>
    /// <returns>The directory path of a deployment of Zero Install; <c>null</c> if none was found.</returns>
    /// <remarks>Use <see cref="Find"/> to also find unregistered deployments in well-known locations.</remarks>
    public static string? GetPath(bool machineWide)
        => ReadInstallLocation(machineWide) is {Length: > 0} path && TryFindExecutable(path) != null
            ? path
            : null;

    /// <summary>
    /// Tries to find a deployment of Zero Install, falling back to well-known locations if no registered deployment is found.
    /// </summary>
    /// <param name="needsMachineWide"><c>true</c> if a machine-wide deployment is required; <c>false</c> if a user-specific deployment will also do.</param>
    /// <returns>The directory path of a deployment of Zero Install; <c>null</c> if none was found.</returns>
    /// <remarks>Prefers registered deployments over unregistered ones and user-specific deployments over machine-wide ones.</remarks>
    public static string? Find(bool needsMachineWide = false)
        => GetCandidates(needsMachineWide).FirstOrDefault(path => TryFindExecutable(path) != null);

    /// <summary>
    /// Lists the paths where a deployment of Zero Install may be located, in order of preference.
    /// </summary>
    private static IEnumerable<string> GetCandidates(bool needsMachineWide)
    {
        bool[] scopes = needsMachineWide ? [true] : [false, true];

        foreach (bool machineWide in scopes)
        {
            if (ReadInstallLocation(machineWide) is {Length: > 0} path)
                yield return path;
        }

        foreach (bool machineWide in scopes)
        {
            foreach (string path in GetWellKnownPaths(machineWide))
                yield return path;
        }
    }

    /// <summary>
    /// Lists the well-known locations where Zero Install may be deployed without being registered.
    /// </summary>
    private static IEnumerable<string> GetWellKnownPaths(bool machineWide)
    {
        if (!WindowsUtils.IsWindows && !UnixUtils.IsUnix) yield break;

        if (UnixUtils.IsMacOSX) yield return GetDefaultPath(machineWide, appBundle: true);
        yield return GetDefaultPath(machineWide);
    }

    /// <summary>
    /// Gets the default location for deploying Zero Install.
    /// </summary>
    /// <param name="machineWide"><c>true</c> to get a machine-wide location; <c>false</c> to get a user-specific location.</param>
    /// <param name="appBundle">Whether the deployment is a macOS app bundle. Ignored on other platforms.</param>
    /// <exception cref="PlatformNotSupportedException">The current platform is not supported.</exception>
    public static string GetDefaultPath(bool machineWide, bool appBundle = false)
    {
        if (WindowsUtils.IsWindows)
        {
            string programFiles = machineWide
                ? WindowsUtils.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                : Paths.Combine(WindowsUtils.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Programs");
            return Paths.Combine(programFiles, "Zero Install");
        }
        else if (UnixUtils.IsMacOSX && appBundle)
        {
            string applications = machineWide ? "/Applications" : Paths.Combine(Locations.HomeDir, "Applications");
            return Paths.Combine(applications, "Zero Install.app");
        }
        else if (UnixUtils.IsUnix)
            return machineWide ? "/usr/local/lib/zero-install" : Paths.Combine(Locations.HomeDir, ".zero-install");
        else throw new PlatformNotSupportedException();
    }

    /// <summary>
    /// Like <see cref="FindExecutable"/>, but logs invalid paths instead of throwing.
    /// </summary>
    private static string? TryFindExecutable(string deploymentPath, bool gui = false)
    {
        try
        {
            return FindExecutable(deploymentPath, gui);
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or ArgumentException)
        {
            Log.Warn($"Invalid Zero Install deployment path: {deploymentPath}", ex);
            return null;
        }
        #endregion
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
            machineWide ? "/etc" : Locations.UserConfigDir,
            AppName,
            fileName);

    private static string GetSaveConfigFilePath(string fileName, bool machineWide)
    {
        string path = GetConfigFilePath(fileName, machineWide);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }
}
