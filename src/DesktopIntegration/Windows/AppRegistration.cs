// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Win32;
using NanoByte.Common.Collections;
using NanoByte.Common.Native;
using ZeroInstall.Model;
using ZeroInstall.Model.Capabilities;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains control logic for applying <see cref="Model.Capabilities.AppRegistration"/> on Windows systems.
    /// </summary>
    public static class AppRegistration
    {
        #region Constants
        /// <summary>Prepended before <see cref="Model.Capabilities.AppRegistration.CapabilityRegPath"/>. This prevents conflicts with non-Zero Install installations.</summary>
        private const string CapabilityPrefix = @"SOFTWARE\Zero Install\Applications\";

        /// <summary>The HKLM registry key for registering applications as candidates for default programs.</summary>
        public const string RegKeyMachineRegisteredApplications = @"SOFTWARE\RegisteredApplications";

        /// <summary>The registry value name for the application name.</summary>
        public const string RegValueAppName = "ApplicationName";

        /// <summary>The registry value name for the application description.</summary>
        public const string RegValueAppDescription = "ApplicationDescription";

        /// <summary>The registry value name for the application icon.</summary>
        public const string RegValueAppIcon = "ApplicationIcon";

        /// <summary>The registry subkey containing <see cref="Model.Capabilities.FileType"/> references.</summary>
        public const string RegSubKeyFileAssocs = "FileAssociations";

        /// <summary>The registry subkey containing <see cref="Model.Capabilities.UrlProtocol"/> references.</summary>
        public const string RegSubKeyUrlAssocs = "URLAssociations";

        /// <summary>The registry subkey containing <see cref="Model.Capabilities.DefaultProgram"/> references.</summary>
        public const string RegSubKeyStartMenu = "StartMenu";
        #endregion

        #region Apply
        /// <summary>
        /// Applies application registration to the current system.
        /// </summary>
        /// <param name="target">The application being integrated.</param>
        /// <param name="appRegistration">The registration information to be applied.</param>
        /// <param name="verbCapabilities">The capabilities that the application is to be registered with.</param>
        /// <param name="machineWide">Apply the registration machine-wide instead of just for the current user.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="appRegistration"/> or <paramref name="verbCapabilities"/> is invalid.</exception>
        public static void Register(FeedTarget target, Model.Capabilities.AppRegistration appRegistration, IEnumerable<VerbCapability> verbCapabilities, IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (appRegistration == null) throw new ArgumentNullException(nameof(appRegistration));
            if (verbCapabilities == null) throw new ArgumentNullException(nameof(verbCapabilities));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            if (string.IsNullOrEmpty(appRegistration.ID)) throw new InvalidDataException("Missing ID");
            if (string.IsNullOrEmpty(appRegistration.CapabilityRegPath)) throw new InvalidDataException("Invalid CapabilityRegPath");

            var hive = machineWide ? Registry.LocalMachine : Registry.CurrentUser;

            // TODO: Handle appRegistration.X64
            using (var capabilitiesKey = hive.CreateSubKeyChecked( /*CapabilityPrefix +*/ appRegistration.CapabilityRegPath))
            {
                capabilitiesKey.SetValue(RegValueAppName, target.Feed.Name ?? "");
                capabilitiesKey.SetValue(RegValueAppDescription, target.Feed.Descriptions.GetBestLanguage(CultureInfo.CurrentUICulture) ?? "");

                // Set icon if available
                var icon = target.Feed.Icons.GetIcon(Icon.MimeTypeIco);
                if (icon != null) capabilitiesKey.SetValue(RegValueAppIcon, iconStore.GetPath(icon) + ",0");

                verbCapabilities = verbCapabilities.ToArray();

                using (var fileAssocsKey = capabilitiesKey.CreateSubKeyChecked(RegSubKeyFileAssocs))
                {
                    foreach (var fileType in verbCapabilities.OfType<Model.Capabilities.FileType>().Except(x => string.IsNullOrEmpty(x.ID)))
                    {
                        foreach (var extension in fileType.Extensions.Except(x => string.IsNullOrEmpty(x.Value)))
                            fileAssocsKey.SetValue(extension.Value, RegistryClasses.Prefix + fileType.ID);
                    }
                }

                using (var urlAssocsKey = capabilitiesKey.CreateSubKeyChecked(RegSubKeyUrlAssocs))
                {
                    foreach (var urlProtocol in verbCapabilities.OfType<Model.Capabilities.UrlProtocol>())
                    {
                        foreach (var prefix in urlProtocol.KnownPrefixes)
                            urlAssocsKey.SetValue(prefix.Value, RegistryClasses.Prefix + urlProtocol.ID);
                    }
                }

                using var startMenuKey = capabilitiesKey.CreateSubKeyChecked(RegSubKeyStartMenu);
                foreach (var defaultProgram in verbCapabilities.OfType<Model.Capabilities.DefaultProgram>().Except(x => string.IsNullOrEmpty(x.ID) || string.IsNullOrEmpty(x.Service)))
                    startMenuKey.SetValue(defaultProgram.Service, defaultProgram.ID);
            }

            using var regAppsKey = hive.CreateSubKeyChecked(RegKeyMachineRegisteredApplications);
            regAppsKey.SetValue(appRegistration.ID, /*CapabilityPrefix +*/ appRegistration.CapabilityRegPath);
        }
        #endregion

        #region Remove
        /// <summary>
        /// Removes application registration from the current system.
        /// </summary>
        /// <param name="appRegistration">The registration information to be removed.</param>
        /// <param name="machineWide">Apply the registration machine-wide instead of just for the current user.</param>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="appRegistration"/>.</exception>
        public static void Unregister(Model.Capabilities.AppRegistration appRegistration, bool machineWide)
        {
            #region Sanity checks
            if (appRegistration == null) throw new ArgumentNullException(nameof(appRegistration));
            #endregion

            if (string.IsNullOrEmpty(appRegistration.ID)) throw new InvalidDataException("Missing ID");
            if (string.IsNullOrEmpty(appRegistration.CapabilityRegPath)) throw new InvalidDataException("Invalid CapabilityRegPath");

            var hive = machineWide ? Registry.LocalMachine : Registry.CurrentUser;

            using (var regAppsKey = hive.OpenSubKey(RegKeyMachineRegisteredApplications, writable: true))
                regAppsKey?.DeleteValue(appRegistration.ID, throwOnMissingValue: false);

            // TODO: Handle appRegistration.X64
            hive.DeleteSubKeyTree( /*CapabilityPrefix +*/ appRegistration.CapabilityRegPath, throwOnMissingSubKey: false);
        }
        #endregion
    }
}
