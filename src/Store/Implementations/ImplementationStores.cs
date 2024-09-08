// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;

#if NETFRAMEWORK
using NanoByte.Common.Native;
#endif

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Manages implementation store directories and provides <see cref="IImplementationStore"/> instances.
/// </summary>
public static class ImplementationStores
{
    /// <summary>
    /// Creates an <see cref="IImplementationStore"/> instance that uses the default cache locations (based on <see cref="ImplementationStores"/>.
    /// </summary>
    /// <param name="handler">A callback object used when the user is to be informed about progress or asked questions.</param>
    /// <exception cref="IOException">There was a problem accessing a configuration file or one of the stores.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file or one of the stores was not permitted.</exception>
    public static IImplementationStore Default(ITaskHandler handler)
    {
        var stores = new List<IImplementationStore>();

        foreach (string path in GetDirectories())
        {
            try
            {
                stores.Add(new ImplementationStore(path, handler));
            }
            #region Error handling
            catch (IOException ex)
            {
                // Wrap exception to add context information
                throw new IOException(ProblemAccessingStoreMessage(path), ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Wrap exception to add context information
                throw new UnauthorizedAccessException(ProblemAccessingStoreMessage(path), ex);
            }
            #endregion
        }

#if NETFRAMEWORK
            if (WindowsUtils.IsWindowsNT && !Locations.IsPortable)
                stores.Add(new ServiceImplementationStore());
#endif

        return new CompositeImplementationStore(stores);
    }

    /// <summary>
    /// Returns a list of paths for implementation directories as defined by configuration files including the default locations.
    /// </summary>
    /// <param name="serviceMode"><c>true</c> to exclude the default location in the user profile, e.g., for system services.</param>
    /// <remarks>Multiple configuration files apply cumulatively. I.e., directories from both the user config and the system config are used.</remarks>
    /// <exception cref="IOException">There was a problem accessing a configuration file or one of the stores.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file was not permitted.</exception>
    [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Reads data from a config file with no caching")]
    public static IEnumerable<string> GetDirectories(bool serviceMode = false)
    {
        if (!serviceMode)
        {
            // Add the user cache to have a reliable fallback location for storage
            yield return Locations.GetCacheDirPath("0install.net", machineWide: false, resource: "implementations");
        }

        // Add the system cache when not in portable mode
        if (!Locations.IsPortable)
        {
            string? systemCache;
            try
            {
                systemCache = Locations.GetCacheDirPath("0install.net", machineWide: true, resource: "implementations");
            }
            catch (UnauthorizedAccessException)
            { // Standard users cannot create machine-wide directories, only use them if they already exist
                systemCache = null;
            }
            if (systemCache != null) yield return systemCache;
        }

        // Add configured cache locations
        foreach (string configFile in Locations.GetLoadConfigPaths("0install.net", true, "injector", "implementation-dirs"))
        {
            foreach (string path in GetDirectories(configFile))
                yield return path;
        }
    }

    /// <summary>
    /// Returns a list of custom implementation directories in the current user configuration.
    /// </summary>
    /// <exception cref="IOException">There was a problem accessing a configuration file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file was not permitted.</exception>
    [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "May throw exceptions")]
    public static IEnumerable<string> GetUserDirectories()
        => GetDirectories(GetUserConfigFile());

    /// <summary>
    /// Sets the list of custom implementation directories in the current user configuration.
    /// </summary>
    /// <param name="paths">The list of implementation directories to set.</param>
    /// <exception cref="IOException">There was a problem writing a configuration file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file was not permitted.</exception>
    public static void SetUserDirectories(IEnumerable<string> paths)
        => SetDirectories(GetUserConfigFile(), paths);

    private static string GetUserConfigFile()
        => Locations.GetSaveConfigPath("0install.net", isFile: true, "injector", "implementation-dirs");

    /// <summary>
    /// Returns a list of custom implementation directories in the current machine-wide configuration.
    /// </summary>
    /// <exception cref="IOException">There was a problem accessing a configuration file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file was not permitted.</exception>
    [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "May throw exceptions")]
    public static IEnumerable<string> GetMachineWideDirectories()
        => GetDirectories(GetMachineWideConfigFile());

    /// <summary>
    /// Sets the list of custom implementation directories in the current machine-wide configuration.
    /// </summary>
    /// <param name="paths">The list of implementation directories to set.</param>
    /// <exception cref="IOException">There was a problem writing a configuration file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file was not permitted.</exception>
    public static void SetMachineWideDirectories(IEnumerable<string> paths)
        => SetDirectories(GetMachineWideConfigFile(), paths);

    private static string GetMachineWideConfigFile()
        => Locations.GetSaveSystemConfigPath("0install.net", true, "injector", "implementation-dirs");

    /// <summary>
    /// Returns a list of implementation directories in a specific configuration file.
    /// </summary>
    /// <param name="configPath">The path of the configuration file to read.</param>
    /// <exception cref="IOException">There was a problem accessing <paramref name="configPath"/>.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to <paramref name="configPath"/> was not permitted.</exception>
    private static IEnumerable<string> GetDirectories(string configPath)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(configPath)) throw new ArgumentNullException(nameof(configPath));
        #endregion

        if (!File.Exists(configPath)) yield break;

        string[] ReadAllLines()
        {
            using (new AtomicRead(configPath))
                return File.ReadAllLines(configPath, Encoding.UTF8);
        }

        foreach (string path in
                 from line in ReadAllLines()
                 where !line.StartsWith("#") && !string.IsNullOrEmpty(line)
                 select Environment.ExpandEnvironmentVariables(line))
        {
            string result = path;
            try
            {
                if (!Path.IsPathRooted(path))
                { // Allow relative paths only for portable installations
                    if (Locations.IsPortable) result = Path.Combine(Locations.PortableBase, path);
                    else throw new IOException(string.Format(Resources.NonRootedPathInConfig, path, configPath));
                }
            }
            #region Error handling
            catch (ArgumentException ex)
            {
                // Wrap exception to add context information
                throw new IOException(ProblemAccessingStoreMessage(path), ex);
            }
            #endregion

            yield return result;
        }
    }

    private static string ProblemAccessingStoreMessage(string path)
        => string.Format(Resources.ProblemAccessingStore, path, $"0install store remove-dir {path.EscapeArgument()}");

    /// <summary>
    /// Sets the list of implementation directories in a specific configuration file.
    /// </summary>
    /// <param name="configPath">The path of the configuration file to write.</param>
    /// <param name="paths">The list of implementation directories to set.</param>
    /// <exception cref="IOException">There was a problem writing <paramref name="configPath"/>.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to <paramref name="configPath"/> was not permitted.</exception>
    private static void SetDirectories(string configPath, IEnumerable<string> paths)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(configPath)) throw new ArgumentNullException(nameof(configPath));
        if (paths == null) throw new ArgumentNullException(nameof(paths));
        #endregion

        using var atomic = new AtomicWrite(configPath);
        using (var configFile = new StreamWriter(atomic.WritePath, append: false, EncodingUtils.Utf8) {NewLine = "\n"})
        {
            foreach (string path in paths)
                configFile.WriteLine(path);
        }
        atomic.Commit();
    }
}
