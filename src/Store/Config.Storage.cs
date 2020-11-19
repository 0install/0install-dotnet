// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IniParser;
using IniParser.Exceptions;
using IniParser.Model;
using Microsoft.Win32;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Values;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Properties;

#if NETFRAMEWORK
using System.Configuration;
#endif

#if NETSTANDARD
using Microsoft.Extensions.Configuration;
#endif

namespace ZeroInstall.Store
{
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

#if NETFRAMEWORK
            config.ReadFromAppSettings();
#endif
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
            catch (IOException ex)
            {
                Log.Error(ex);
                return new Config();
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex);
                return new Config();
            }
            catch (InvalidDataException ex)
            {
                Log.Error(ex);
                return new Config();
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

#if NETSTANDARD
        /// <summary>
        /// Gets the settings from a .NET Extensions <see cref="IConfiguration"/> provider.
        /// </summary>
        /// <returns>The loaded <see cref="Config"/>.</returns>
        [CLSCompliant(false)]
        public static Config From(IConfiguration configuration)
        {
            var config = new Config();
            foreach ((string key, var pointer) in config._metaData)
            {
                string? value = configuration[key];
                if (value != null) pointer.Value = value;
            }
            return config;
        }
#endif

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
            using (var writer = new StreamWriter(atomic.WritePath, append: false, encoding: FeedUtils.Encoding))
                new StreamIniDataParser().WriteData(writer, _iniData);
            atomic.Commit();
        }

        /// <summary>
        /// Reads settings from INI files on the disk and transfers them to properties.
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
        /// Reads settings from an INI file on the disk and transfers them to properties.
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
            foreach (var (key, property) in _metaData)
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
            if (_iniData == null) _iniData = new IniData();
            _iniData.Sections.RemoveSection("__global__section__"); // Throw away section-less data

            if (!_iniData.Sections.ContainsSection(GlobalSection)) _iniData.Sections.AddSection(GlobalSection);
            var global = _iniData[GlobalSection];

            foreach (var property in _metaData)
            {
                string key = property.Key;
                if (property.Value.NeedsEncoding) key += Base64Suffix;

                if (property.Value.IsDefaultValue || property.Value.Value == null)
                    global.RemoveKey(key);
                else
                {
                    global[key] = property.Value.NeedsEncoding
                        ? property.Value.Value.Base64Utf8Encode()
                        : property.Value.Value;
                }
            }
        }

#if NETFRAMEWORK
        /// <summary>
        /// Reads settings from <see cref="ConfigurationManager.AppSettings"/> and transfers them to properties.
        /// </summary>
        private void ReadFromAppSettings()
        {
            foreach (var property in _metaData)
            {
                string value = ConfigurationManager.AppSettings[property.Key];
                if (!string.IsNullOrEmpty(value))
                    property.Value.Value = value;
            }
        }
#endif

        /// <summary>
        /// Reads settings from Windows policy registry keys and transfers them to properties.
        /// </summary>
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
        /// Reads settings from a Windows registry key and transfers them to properties.
        /// </summary>
        private void ReadFromRegistry(RegistryKey registryKey)
        {
            Log.Debug("Loading config from: " + registryKey);

            foreach (var property in _metaData)
            {
                string key = property.Key;
                var data = registryKey.GetValue(key);
                if (data != null)
                {
                    string value = data.ToString();
                    try
                    {
                        property.Value.Value = value;
                    }
                    #region Error handling
                    catch (FormatException ex)
                    {
                        // Wrap exception to add context information
                        throw new InvalidDataException(string.Format(Resources.ProblemLoadingConfigValue, property.Key, registryKey.Name), ex);
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// Creates a deep copy of this <see cref="Config"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Config"/>.</returns>
        public Config Clone()
        {
            var newConfig = new Config();
            foreach (var property in _metaData)
                newConfig.SetOption(property.Key, GetOption(property.Key));
            return newConfig;
        }

        /// <summary>
        /// Returns the keys and values of all contained settings.
        /// </summary>
        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var property in _metaData)
                builder.AppendLine(property.Key + " = " + (property.Value.NeedsEncoding ? "***" : property.Value.Value));
            return builder.ToString();
        }

        /// <inheritdoc/>
        public bool Equals(Config other)
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
}
