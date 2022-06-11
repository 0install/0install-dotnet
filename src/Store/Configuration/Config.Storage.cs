// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using System.Text;
using IniParser;
using IniParser.Exceptions;
using IniParser.Model;
using Microsoft.Win32;
using NanoByte.Common.Native;
using NanoByte.Common.Values;

#if NETFRAMEWORK
using System.Configuration;
#endif

namespace ZeroInstall.Store.Configuration;

partial class Config
{
    /// <summary>
    /// Retrieves the string representation of an option identified by a key.
    /// </summary>
    /// <param name="key">The key of the option to retrieve.</param>
    /// <returns>The string representation of the the option.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="key"/> is invalid.</exception>
    public string GetOption(string key)
        => _metaData[key].Value;

    /// <summary>
    /// Sets an option identified by a key.
    /// </summary>
    /// <param name="key">The key of the option to set.</param>
    /// <param name="value">A string representation of the option.</param>
    /// <exception cref="KeyNotFoundException"><paramref name="key"/> is invalid.</exception>
    /// <exception cref="FormatException"><paramref name="value"/> is invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">This option is controlled by a group policy and can therefore not be modified.</exception>
    public void SetOption(string key, string value)
    {
        if (IsOptionLocked(key))
            throw new UnauthorizedAccessException(Resources.OptionLockedByPolicy);

        _metaData[key].Value = value;
    }

    /// <summary>
    /// Determines whether an option is locked by a group policy.
    /// </summary>
    /// <param name="key">The key of the option to check.</param>
    public static bool IsOptionLocked(string key)
    {
        bool HasPolicyIn(RegistryKey root)
        {
            using var registryKey = root.OpenSubKey(RegistryPolicyPath, writable: false);
            return registryKey?.GetValue(key) != null;
        }

        // Extracted to separate function to prevent type load error on non-Windows OSes
        bool HasPolicy() => HasPolicyIn(Registry.CurrentUser) || HasPolicyIn(Registry.LocalMachine);

        return WindowsUtils.IsWindowsNT && HasPolicy();
    }

    /// <summary>
    /// Resets an option identified by a key to its default value.
    /// </summary>
    /// <param name="key">The key of the option to reset.</param>
    /// <exception cref="KeyNotFoundException"><paramref name="key"/> is invalid.</exception>
    public void ResetOption(string key)
        => SetOption(key, _metaData[key].DefaultValue);

    private const string RegistryPolicyPath = @"SOFTWARE\Policies\Zero Install";

    /// <summary>
    /// Aggregates the settings from all applicable INI files listed by <see cref="Locations.GetLoadConfigPaths"/>.
    /// </summary>
    /// <returns>The loaded <see cref="Config"/>.</returns>
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
    /// <returns>The loaded <see cref="Config"/>.</returns>
    /// <exception cref="IOException">A problem occurred while reading the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing the config data.</exception>
    public static Config Load(string path)
    {
        Log.Debug("Loading Config from: " + path);

        var config = new Config();
        config.ReadFromIniFile(path);
        return config;
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

        Log.Debug("Saving Config to: " + path);

        using var atomic = new AtomicWrite(path);
        using (var writer = new StreamWriter(atomic.WritePath, append: false, EncodingUtils.Utf8))
            new StreamIniDataParser().WriteData(writer, _iniData);
        atomic.Commit();
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

    private const string GlobalSection = "global";
    private const string Base64Suffix = "_base64";

    /// <summary>Stores the original INI data so that unknown values are preserved on re<see cref="Save()"/>ing.</summary>
    [NonSerialized]
    private IniData? _iniData;

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
            using var registryKey = root.OpenSubKey(RegistryPolicyPath, writable: false);
            if (registryKey != null)
                ReadFromRegistry(registryKey);
        }
    }

    /// <summary>
    /// Reads settings from a Windows registry key.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void ReadFromRegistry(RegistryKey registryKey)
    {
        Log.Debug("Loading config from: " + registryKey);

        foreach ((string key, var property) in _metaData)
        {
            string? value = registryKey.GetValue(key)?.ToString();
            if (value == null) continue;

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

#if NETFRAMEWORK
    /// <summary>
    /// Reads settings from <see cref="ConfigurationManager.AppSettings"/>, if they have not already been set to non-default values.
    /// </summary>
    public void ReadFromAppSettings()
    {
        var appSettings = ConfigurationManager.AppSettings;
        foreach ((string key, var property) in _metaData)
        {
            if (property.IsDefaultValue)
            {
                string value = appSettings[key];
                if (!string.IsNullOrEmpty(value))
                    property.Value = value;
            }
        }
    }
#endif

    /// <summary>
    /// Creates a deep copy of this <see cref="Config"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Config"/>.</returns>
    public Config Clone()
    {
        var newConfig = new Config();
        foreach ((string key, var property) in _metaData)
            newConfig._metaData[key].Value = property.Value;
        return newConfig;
    }

    /// <inheritdoc/>
    public bool Equals(Config? other)
        => other != null
        && _metaData.All(property => property.Value.Value == other.GetOption(property.Key));

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj == this) return true;
        return obj is Config config && Equals(config);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
        => _metaData.GetUnsequencedHashCode(
            new KeyEqualityComparer<PropertyPointer<string>, string>(x => x.Value));
}
