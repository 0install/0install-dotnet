// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Win32;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Native;
using ZeroInstall.Model;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains control logic for applying <see cref="Model.Capabilities.FileType"/> and <see cref="AccessPoints.FileType"/> on Windows systems.
    /// </summary>
    public static class FileType
    {
        #region Constants
        /// <summary>The HKCU/HKLM registry key backing HKCR.</summary>
        public const string RegKeyOverrides = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts";

        /// <summary>The registry value name for friendly type name storage.</summary>
        public const string RegValueFriendlyName = "FriendlyTypeName";

        /// <summary>The registry value name for application user model IDs (used by the Windows 7 taskbar).</summary>
        public const string RegValueAppUserModelID = "AppUserModelID";

        /// <summary>The registry value name for MIME type storage.</summary>
        public const string RegValueContentType = "Content Type";

        /// <summary>The registry value name for perceived type storage.</summary>
        public const string RegValuePerceivedType = "PerceivedType";

        /// <summary>The registry subkey containing "open with" ProgID references.</summary>
        public const string RegSubKeyOpenWith = "OpenWithProgIDs";

        /// <summary>The registry subkey below HKEY_CLASSES_ROOT that contains MIME type to extension mapping.</summary>
        public const string RegSubKeyMimeType = @"MIME\Database\Content Type";

        /// <summary>The registry value name for a MIME type extension association.</summary>
        public const string RegValueExtension = "Extension";
        #endregion

        #region Register
        /// <summary>
        /// Registers a file type in the current system.
        /// </summary>
        /// <param name="target">The application being integrated.</param>
        /// <param name="fileType">The file type to register.</param>
        /// <param name="machineWide">Register the file type machine-wide instead of just for the current user.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <param name="accessPoint">Indicates that the file associations shall become default handlers for their respective types.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="fileType"/> is invalid.</exception>
        public static void Register(FeedTarget target, Model.Capabilities.FileType fileType, IIconStore iconStore, bool machineWide, bool accessPoint = false)
        {
            #region Sanity checks
            if (fileType == null) throw new ArgumentNullException(nameof(fileType));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            if (string.IsNullOrEmpty(fileType.ID)) throw new InvalidDataException("Missing ID");

            using var classesKey = RegistryClasses.OpenHive(machineWide);

            // Register ProgID
            using (var progIDKey = classesKey.CreateSubKeyChecked(RegistryClasses.Prefix + fileType.ID))
            {
                progIDKey.SetValue("", fileType.Descriptions.GetBestLanguage(CultureInfo.CurrentUICulture) ?? fileType.ID);
                progIDKey.SetValue(accessPoint ? RegistryClasses.PurposeFlagAccessPoint : RegistryClasses.PurposeFlagCapability, "");
                RegistryClasses.Register(progIDKey, target, fileType, iconStore, machineWide);
            }

            foreach (var extension in fileType.Extensions.Except(x => string.IsNullOrEmpty(x.Value)))
            {
                // Register extensions
                using (var extensionKey = classesKey.CreateSubKeyChecked(extension.Value))
                {
                    if (!string.IsNullOrEmpty(extension.MimeType)) extensionKey.SetValue(RegValueContentType, extension.MimeType);
                    if (!string.IsNullOrEmpty(extension.PerceivedType)) extensionKey.SetValue(RegValuePerceivedType, extension.PerceivedType);

                    using (var openWithKey = extensionKey.CreateSubKeyChecked(RegSubKeyOpenWith))
                        openWithKey.SetValue(RegistryClasses.Prefix + fileType.ID, "");

                    if (accessPoint)
                    {
                        if (!machineWide && WindowsUtils.IsWindowsVista)
                        { // Windows Vista and later store per-user file extension overrides
                            using var overridesKey = Registry.CurrentUser.OpenSubKeyChecked(RegKeyOverrides, writable: true);
                            using var extensionOverrideKey = overridesKey.CreateSubKeyChecked(extension.Value);
                            // Only mess with this part of the registry when necessary
                            bool alreadySet;
                            using (var userChoiceKey = extensionOverrideKey.OpenSubKey("UserChoice", writable: false))
                            {
                                if (userChoiceKey == null) alreadySet = false;
                                else alreadySet = ((userChoiceKey.GetValue("Progid") ?? "").ToString() == RegistryClasses.Prefix + fileType.ID);
                            }

                            if (!alreadySet)
                            {
                                // Must delete and recreate instead of direct modification due to wicked ACLs
                                extensionOverrideKey.DeleteSubKeyTree("UserChoice", throwOnMissingSubKey: false);

                                try
                                {
                                    using var userChoiceKey = extensionOverrideKey.CreateSubKeyChecked("UserChoice");
                                    userChoiceKey.SetValue("Progid", RegistryClasses.Prefix + fileType.ID);
                                }
                                catch (UnauthorizedAccessException ex)
                                {
                                    // Windows may try to prevent modifications to this key
                                    Log.Debug(ex);
                                }
                            }
                        }
                        else extensionKey.SetValue("", RegistryClasses.Prefix + fileType.ID);
                    }
                }

                // Register MIME types
                if (!string.IsNullOrEmpty(extension.MimeType))
                {
                    using var mimeKey = classesKey.CreateSubKeyChecked($@"{RegSubKeyMimeType}\{extension.MimeType}");
                    mimeKey.SetValue(RegValueExtension, extension.Value);
                }
            }
        }
        #endregion

        #region Unregister
        /// <summary>
        /// Unregisters a file type in the current system.
        /// </summary>
        /// <param name="fileType">The file type to remove.</param>
        /// <param name="machineWide">Unregister the file type machine-wide instead of just for the current user.</param>
        /// <param name="accessPoint">Indicates that the file associations were default handlers for their respective types.</param>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="fileType"/> is invalid.</exception>
        public static void Unregister(Model.Capabilities.FileType fileType, bool machineWide, bool accessPoint = false)
        {
            #region Sanity checks
            if (fileType == null) throw new ArgumentNullException(nameof(fileType));
            #endregion

            if (string.IsNullOrEmpty(fileType.ID)) throw new InvalidDataException("Missing ID");

            using var classesKey = RegistryClasses.OpenHive(machineWide);
            foreach (var extension in fileType.Extensions.Except(extension => string.IsNullOrEmpty(extension.Value)))
            {
                // Unregister MIME types
                // if (!string.IsNullOrEmpty(extension.MimeType))
                // {
                //     using (var extensionKey = classesKey.CreateSubKeyChecked(extension.Value))
                //     {
                //         // TODO: Restore previous default
                //         extensionKey.SetValue("", fileType.PreviousID);
                //     }
                //
                //     if (!machineWide && WindowsUtils.IsWindowsVista && !WindowsUtils.IsWindows8)
                //     {
                //         // Windows Vista and later store per-user file extension overrides, Windows 8 blocks programmatic modification with hash values
                //         using (var overridesKey = hive.OpenSubKey(RegKeyOverrides, true))
                //         using (var extensionOverrideKey = overridesKey.CreateSubKeyChecked(extension.Value))
                //         {
                //             // Must delete and recreate instead of direct modification due to wicked ACLs
                //             extensionOverrideKey.DeleteSubKeyTree("UserChoice", throwOnMissingSubKey: false);
                //             using (var userChoiceKey = extensionOverrideKey.CreateSubKeyChecked("UserChoice"))
                //                 userChoiceKey.SetValue("ProgID", fileType.PreviousID);
                //         }
                //     }
                // }

                // Unregister extensions
                using var extensionKey = classesKey.OpenSubKey(extension.Value, writable: true);
                if (extensionKey != null)
                {
                    using var openWithKey = extensionKey.OpenSubKey(RegSubKeyOpenWith, writable: true);
                    openWithKey?.DeleteValue(RegistryClasses.Prefix + fileType.ID, throwOnMissingValue: false);
                }

                if (accessPoint)
                {
                    // TODO: Restore previous default
                }
            }

            // Remove appropriate purpose flag and check if there are others
            bool otherFlags;
            using (var progIDKey = classesKey.OpenSubKey(RegistryClasses.Prefix + fileType.ID, writable: true))
            {
                if (progIDKey == null) otherFlags = false;
                else
                {
                    progIDKey.DeleteValue(accessPoint ? RegistryClasses.PurposeFlagAccessPoint : RegistryClasses.PurposeFlagCapability, throwOnMissingValue: false);
                    otherFlags = progIDKey.GetValueNames().Any(name => name.StartsWith(RegistryClasses.PurposeFlagPrefix));
                }
            }

            // Delete ProgID if there are no other references
            if (!otherFlags)
                classesKey.DeleteSubKeyTree(RegistryClasses.Prefix + fileType.ID, throwOnMissingSubKey: false);
        }
        #endregion
    }
}
