// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using NanoByte.Common;
using NanoByte.Common.Native;
using ZeroInstall.Model;
using ZeroInstall.Model.Capabilities;
using ZeroInstall.Publish.Properties;

namespace ZeroInstall.Publish.Capture
{
    partial class SnapshotDiff
    {
        /// <summary>
        /// Retrieves data about registered applications.
        /// </summary>
        /// <param name="commandMapper">Provides best-match command-line to <see cref="Command"/> mapping.</param>
        /// <param name="capabilities">The capability list to add the collected data to.</param>
        /// <param name="appName">Is set to the name of the application as displayed to the user; unchanged if the name was not found.</param>
        /// <param name="appDescription">Is set to a user-friendly description of the application; unchanged if the name was not found.</param>
        /// <exception cref="IOException">There was an error accessing the registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
        public AppRegistration? GetAppRegistration(CommandMapper commandMapper, CapabilityList capabilities, ref string? appName, ref string? appDescription)
        {
            #region Sanity checks
            if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));
            if (commandMapper == null) throw new ArgumentNullException(nameof(commandMapper));
            #endregion

            // Ambiguity warnings
            if (RegisteredApplications.Count == 0)
                return null;
            if (RegisteredApplications.Count > 1)
                Log.Warn(Resources.MultipleRegisteredAppsDetected);

            // Get registry path pointer
            string appRegName = RegisteredApplications[0];
            string? capabilitiesRegPath = RegistryUtils.GetString(@"HKEY_LOCAL_MACHINE\" + DesktopIntegration.Windows.AppRegistration.RegKeyMachineRegisteredApplications, appRegName);
            if (string.IsNullOrEmpty(capabilitiesRegPath))
                return null;

            try
            {
                using var capsKey = RegistryUtils.OpenHklmKey(capabilitiesRegPath, out _);
                if (string.IsNullOrEmpty(appName)) appName = capsKey.GetValue(DesktopIntegration.Windows.AppRegistration.RegValueAppName)?.ToString();
                if (string.IsNullOrEmpty(appDescription)) appDescription = capsKey.GetValue(DesktopIntegration.Windows.AppRegistration.RegValueAppDescription)?.ToString();

                CollectProtocolAssocsEx(capsKey, commandMapper, capabilities);
                CollectFileAssocsEx(capsKey, capabilities);
                // Note: Contenders for StartMenu entries are detected elsewhere
            }
            catch (IOException ex)
            {
                Log.Warn(ex);
            }

            return new()
            {
                ID = appRegName,
                CapabilityRegPath = capabilitiesRegPath
            };
        }

        #region Protocols
        /// <summary>
        /// Collects data about URL protocol handlers indicated by registered application capabilities.
        /// </summary>
        /// <param name="capsKey">A registry key containing capability information for a registered application.</param>
        /// <param name="commandMapper">Provides best-match command-line to <see cref="Command"/> mapping.</param>
        /// <param name="capabilities">The capability list to add the collected data to.</param>
        /// <exception cref="IOException">There was an error accessing the registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
        private static void CollectProtocolAssocsEx(RegistryKey capsKey, CommandMapper commandMapper, CapabilityList capabilities)
        {
            #region Sanity checks
            if (capsKey == null) throw new ArgumentNullException(nameof(capsKey));
            if (commandMapper == null) throw new ArgumentNullException(nameof(commandMapper));
            if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));
            #endregion

            using var urlAssocKey = capsKey.OpenSubKey(DesktopIntegration.Windows.AppRegistration.RegSubKeyUrlAssocs);
            if (urlAssocKey == null) return;

            foreach (string protocol in urlAssocKey.GetValueNames())
            {
                string? progID = urlAssocKey.GetValue(protocol)?.ToString();
                if (string.IsNullOrEmpty(progID)) continue;
                using var progIDKey = Registry.ClassesRoot.OpenSubKey(progID);
                if (progIDKey == null) continue;

                var prefix = new KnownProtocolPrefix {Value = protocol};
                var existing = capabilities.GetCapability<UrlProtocol>(progID);
                if (existing == null)
                {
                    var capability = new UrlProtocol
                    {
                        ID = progID,
                        Descriptions = {progIDKey.GetValue("", defaultValue: "")?.ToString() ?? ""},
                        KnownPrefixes = {prefix}
                    };
                    capability.Verbs.AddRange(GetVerbs(progIDKey, commandMapper));
                    capabilities.Entries.Add(capability);
                }
                else existing.KnownPrefixes.Add(prefix);
            }
        }
        #endregion

        #region File associations
        /// <summary>
        /// Collects data about file associations indicated by registered application capabilities.
        /// </summary>
        /// <param name="capsKey">A registry key containing capability information for a registered application.</param>
        /// <param name="capabilities">The capability list to add the collected data to.</param>
        /// <exception cref="IOException">There was an error accessing the registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
        private static void CollectFileAssocsEx(RegistryKey capsKey, CapabilityList capabilities)
        {
            #region Sanity checks
            if (capsKey == null) throw new ArgumentNullException(nameof(capsKey));
            if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));
            #endregion

            using var fileAssocKey = capsKey.OpenSubKey(DesktopIntegration.Windows.AppRegistration.RegSubKeyFileAssocs);
            if (fileAssocKey == null) return;

            foreach (string extension in fileAssocKey.GetValueNames())
            {
                string? progID = fileAssocKey.GetValue(extension)?.ToString();
                if (!string.IsNullOrEmpty(progID)) AddExtensionToFileType(extension, progID, capabilities);
            }
        }

        /// <summary>
        /// Adds an extension to an existing <see cref="FileType"/>.
        /// </summary>
        /// <param name="extension">The file extension including the leading dot (e.g. ".png").</param>
        /// <param name="progID">The ID of the <see cref="FileType"/> to add the extension to.</param>
        /// <param name="capabilities">The list of capabilities to find existing <see cref="FileType"/>s in.</param>
        private static void AddExtensionToFileType(string extension, string progID, CapabilityList capabilities)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(progID)) throw new ArgumentNullException(nameof(progID));
            if (string.IsNullOrEmpty(extension)) throw new ArgumentNullException(nameof(extension));
            if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));
            #endregion

            // Find the matching existing file type
            var fileType = capabilities.Entries.OfType<FileType>().FirstOrDefault(type => type.ID == progID);

            if (fileType != null)
            {
                // Check if the file type already has the extension and add it if not
                if (!fileType.Extensions.Any(element => StringUtils.EqualsIgnoreCase(element.Value, extension)))
                    fileType.Extensions.Add(new FileTypeExtension {Value = extension.ToLower()});
            }
        }
        #endregion
    }
}
