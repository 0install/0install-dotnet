// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using System.Text;
using IniParser;
using IniParser.Exceptions;
using IniParser.Model;
using Microsoft.Win32;
using NanoByte.Common.Native;

namespace ZeroInstall.Store.Configuration;

partial class Config
{
    private const string AppName = "0install.net", GlobalSection = "global", Base64Suffix = "_base64";
    private static readonly string[] Resource = {"injector", "global"};

    /// <summary>
    /// Aggregates options from all applicable config files and registry locations.
    /// </summary>
    /// <exception cref="IOException">A problem occurred while reading the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing the config data.</exception>
    public static Config Load()
    {
        var config = new Config();
        config.ReadFromFiles();
        config.ReadFromGroupPolicy();
        return config;
    }

    /// <summary>
    /// Tries to aggregates options from all applicable config files and registry locations. Automatically falls back to default values on errors.
    /// </summary>
    /// <returns>The loaded <see cref="Config"/> or default <see cref="Config"/> if there was a problem.</returns>
    public static Config LoadSafe()
    {
        try
        {
            return Load();
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Log.Error("There was a problem loading a configuration file, using default values instead", ex);
            return new();
        }
        #endregion
    }

    /// <summary>
    /// Reads options from config files in default locations and merges them into the config instance.
    /// </summary>
    /// <param name="machineWideOnly"><c>true</c> to only load config from machine-wide locations; <c>false</c> to load from the user profile aswell.</param>
    /// <exception cref="IOException">A problem occurred while reading the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
    /// <exception cref="InvalidDataException">The file contains invalid config values.</exception>
    public void ReadFromFiles(bool machineWideOnly = false)
    {
        var paths = Locations.GetLoadConfigPaths(AppName, isFile: true, Resource);
        if (machineWideOnly) paths = paths.Where(x => x.StartsWith(Locations.UserConfigDir));
        foreach (string path in paths.Reverse()) // Revers order for precedence
            ReadFromFile(path);
    }

    /// <summary>
    /// Stores the last INI data loaded by <see cref="ReadFromFile"/>, so that unknown values can be preserved on re-saving.
    /// </summary>
    [NonSerialized]
    private IniData? _lastIniFromFile;

    /// <summary>
    /// Reads options from a config file and merges them into the config instance.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <exception cref="IOException">A problem occurred while reading the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
    /// <exception cref="InvalidDataException">The file contains invalid config values.</exception>
    public void ReadFromFile(string path)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        #endregion

        IniData iniData;
        using (new AtomicRead(path))
        using (var stream = File.OpenRead(path))
        {
            try
            {
                iniData = new StreamIniDataParser().ReadData(new(stream, Encoding.UTF8));
            }
            #region Error handling
            catch (Exception ex) when (ex is ParsingException or InvalidOperationException)
            {
                // Wrap exception to add context information
                throw new InvalidDataException(string.Format(Resources.ProblemLoadingConfigFile, path), ex);
            }
            #endregion
        }

        ReadFrom(iniData, path);
        _lastIniFromFile = iniData;
    }

    /// <summary>
    /// Reads options from a config file stream and merges them into the config instance.
    /// </summary>
    /// <param name="iniData">The parsed file.</param>
    /// <param name="path">The path of the file <paramref name="iniData"/> was read from. Used for logging.</param>
    /// <exception cref="InvalidDataException">The file contains invalid config values.</exception>
    public void ReadFrom(IniData iniData, string path = "embedded")
    {
        #region Sanity checks
        if (iniData == null) throw new ArgumentNullException(nameof(iniData));
        #endregion

        Log.Debug($"Reading config from file: {path}");

        if (!iniData.Sections.ContainsSection(GlobalSection)) return;
        var global = iniData[GlobalSection];
        foreach ((string key, var property) in _metaData)
        {
            string effectiveKey = property.NeedsEncoding
                ? key + Base64Suffix
                : key;

            if (global.ContainsKey(effectiveKey))
            {
                try
                {
                    property.Value = property.NeedsEncoding
                        ? global[effectiveKey].Base64Utf8Decode()
                        : global[effectiveKey];
                }
                #region Error handling
                catch (FormatException ex)
                {
                    // Wrap exception to add context information
                    throw new InvalidDataException(string.Format(Resources.ProblemLoadingConfigValue, key, path), ex);
                }
                #endregion
            }
        }
    }

    private const string RegistryPolicyPath = @"SOFTWARE\Policies\Zero Install";

    /// <summary>
    /// Reads options from group policies (in the Windows registry) and merges them into the config instance.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Read access to the registry key is not permitted.</exception>
    /// <exception cref="InvalidDataException">The registry key contains invalid config values.</exception>
    public void ReadFromGroupPolicy()
    {
        if (!WindowsUtils.IsWindows) return;

        // Machine policies supersede user policies
        ReadFromGroupPolicy(Registry.CurrentUser);
        ReadFromGroupPolicy(Registry.LocalMachine);
    }

    [SupportedOSPlatform("windows")]
    private void ReadFromGroupPolicy(RegistryKey root)
    {
        try
        {
            using var registryKey = root.TryOpenSubKey(RegistryPolicyPath, writable: false);
            if (registryKey == null) return;

            Log.Debug($"Reading config from registry: {registryKey}");
            foreach ((string key, var property) in _metaData)
            {
                if (registryKey.GetValue(key)?.ToString() is {} value)
                {
                    try
                    {
                        property.Value = value;
                    }
                    #region Error handling
                    catch (FormatException ex)
                    {
                        Log.Warn(string.Format(Resources.ProblemLoadingConfigValue, key, registryKey), ex);
                    }
                    #endregion
                }
            }
        }
        #region Error handling
        catch (Exception ex)
        {
            Log.Warn("Failed to read config from registry", ex);
        }
        #endregion
    }

    /// <summary>
    /// Determines whether an option is locked by a group policy (in the Windows registry).
    /// </summary>
    /// <param name="key">The key of the option to check.</param>
    public static bool IsOptionLocked(string key)
        => WindowsUtils.IsWindows
        && (HasGroupPolicy(Registry.CurrentUser, key)
         || HasGroupPolicy(Registry.LocalMachine, key));

    [SupportedOSPlatform("windows")]
    private static bool HasGroupPolicy(RegistryKey root, string key)
    {
        try
        {
            using var registryKey = root.TryOpenSubKey(RegistryPolicyPath, writable: false);
            return registryKey?.GetValue(key) != null;
        }
        #region Error handling
        catch (Exception ex)
        {
            Log.Warn("Failed to read config from registry", ex);
            return false;
        }
        #endregion
    }

    /// <summary>
    /// Saves the options to a config file in a default location.
    /// </summary>
    /// <param name="machineWide"><c>true</c> to save in a machine-wide location; <c>false</c> to save in the user profile.</param>
    /// <remarks>This method performs an atomic write operation when possible.</remarks>
    /// <exception cref="IOException">A problem occurred while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
    public void Save(bool machineWide = false)
        => Save(machineWide
            ? Locations.GetSaveSystemConfigPath(AppName, isFile: true, Resource)
            : Locations.GetSaveConfigPath(AppName, isFile: true, Resource));

    /// <summary>
    /// Saves the options to a config file.
    /// </summary>
    /// <remarks>This method performs an atomic write operation when possible.</remarks>
    /// <exception cref="IOException">A problem occurred while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
    public void Save(string path)
    {
        var iniData = _lastIniFromFile ?? new();
        iniData.Sections.RemoveSection("__global__section__"); // Throw away section-less data

        if (!iniData.Sections.ContainsSection(GlobalSection)) iniData.Sections.AddSection(GlobalSection);
        var global = iniData[GlobalSection];

        foreach ((string key, var property) in _metaData)
        {
            string effectiveKey = property.NeedsEncoding
                ? key + Base64Suffix
                : key;

            if (property.IsDefaultValue)
                global.RemoveKey(effectiveKey);
            else
            {
                global[effectiveKey] = property.NeedsEncoding
                    ? property.Value.Base64Utf8Encode()
                    : property.Value;
            }
        }

        Log.Debug($"Saving config to file: {path}");
        using var atomic = new AtomicWrite(path);
        using (var writer = new StreamWriter(atomic.WritePath, append: false, EncodingUtils.Utf8))
            new StreamIniDataParser().WriteData(writer, iniData);
        atomic.Commit();
    }
}
