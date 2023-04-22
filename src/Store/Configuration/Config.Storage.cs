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
    private const string GlobalSection = "global", Base64Suffix = "_base64";

    /// <summary>
    /// Stores the original INI data so that unknown values are preserved on re-saving.
    /// </summary>
    [NonSerialized]
    private IniData? _iniData;

    /// <summary>
    /// Aggregates the settings from all applicable INI files listed by <see cref="Locations.GetLoadConfigPaths"/>.
    /// </summary>
    /// <exception cref="IOException">A problem occurred while reading the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing the config data.</exception>
    public static Config Load()
    {
        var config = new Config();

        config.ReadFromIniFiles();
        if (WindowsUtils.IsWindowsNT)
            config.ReadFromRegistry();

        return config;
    }

    /// <summary>
    /// Tries to load the <see cref="Config"/>. Automatically falls back to default values on errors.
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
    /// Loads the settings from a single INI file.
    /// </summary>
    /// <exception cref="IOException">A problem occurred while reading the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing the config data.</exception>
    public static Config Load(string path)
    {
        Log.Debug($"Loading Config from: {path}");

        var config = new Config();
        config.ReadFromIniFile(path);
        return config;
    }

    /// <summary>
    /// Reads settings from INI files on the disk.
    /// </summary>
    private void ReadFromIniFiles()
    {
        var paths = Locations.GetLoadConfigPaths("0install.net", true, "injector", "global");
        foreach (string path in paths.Reverse()) // Read least important first
            ReadFromIniFile(path);
    }

    /// <summary>
    /// Reads settings from an INI file on the disk.
    /// </summary>
    private void ReadFromIniFile(string path)
    {
        try
        {
            using (new AtomicRead(path))
            {
                using var reader = new StreamReader(path, Encoding.UTF8);
                _iniData = new StreamIniDataParser().ReadData(reader);
            }
        }
        #region Error handling
        catch (ParsingException ex)
        {
            // Wrap exception to add context information
            throw new InvalidDataException(string.Format(Resources.ProblemLoadingConfigFile, path), ex);
        }
        #endregion

        if (!_iniData.Sections.ContainsSection(GlobalSection)) return;
        var global = _iniData[GlobalSection];
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
    /// Reads settings from Windows policy registry keys.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void ReadFromRegistry()
    {
        ReadFrom(Registry.LocalMachine);
        ReadFrom(Registry.CurrentUser);

        void ReadFrom(RegistryKey root)
        {
            try
            {
                using var registryKey = root.TryOpenSubKey(RegistryPolicyPath, writable: false);
                if (registryKey != null)
                    ReadFromRegistry(registryKey);
            }
            #region Error handling
            catch (Exception ex)
            {
                Log.Warn("Failed to read config from registry", ex);
            }
            #endregion
        }
    }

    /// <summary>
    /// Reads settings from a Windows registry key.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void ReadFromRegistry(RegistryKey registryKey)
    {
        Log.Debug($"Loading config from: {registryKey}");

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
                    // Wrap exception to add context information
                    throw new InvalidDataException(string.Format(Resources.ProblemLoadingConfigValue, key, registryKey.Name), ex);
                }
                #endregion
            }
        }
    }

    /// <summary>
    /// Determines whether an option is locked by a group policy.
    /// </summary>
    /// <param name="key">The key of the option to check.</param>
    public static bool IsOptionLocked(string key)
    {
        bool HasPolicyIn(RegistryKey root)
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

        // Extracted to separate function to prevent type load error on non-Windows OSes
        bool HasPolicy() => HasPolicyIn(Registry.CurrentUser) || HasPolicyIn(Registry.LocalMachine);

        return WindowsUtils.IsWindowsNT && HasPolicy();
    }

    /// <summary>
    /// Saves the settings to an INI file in the default location in the user profile.
    /// </summary>
    /// <remarks>This method performs an atomic write operation when possible.</remarks>
    /// <exception cref="IOException">A problem occurred while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
    public void Save() => Save(Locations.GetSaveConfigPath("0install.net", true, "injector", "global"));

    /// <summary>
    /// Saves the settings to an INI file.
    /// </summary>
    /// <remarks>This method performs an atomic write operation when possible.</remarks>
    /// <exception cref="IOException">A problem occurred while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
    public void Save(string path)
    {
        TransferToIni();

        Log.Debug($"Saving Config to: {path}");

        using var atomic = new AtomicWrite(path);
        using (var writer = new StreamWriter(atomic.WritePath, append: false, EncodingUtils.Utf8))
            new StreamIniDataParser().WriteData(writer, _iniData);
        atomic.Commit();
    }

    /// <summary>
    /// Transfers settings from properties to <see cref="_iniData"/>.
    /// </summary>
    private void TransferToIni()
    {
        _iniData ??= new();
        _iniData.Sections.RemoveSection("__global__section__"); // Throw away section-less data

        if (!_iniData.Sections.ContainsSection(GlobalSection)) _iniData.Sections.AddSection(GlobalSection);
        var global = _iniData[GlobalSection];

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
    }
}
