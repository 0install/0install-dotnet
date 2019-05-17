// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Linq;
using System.Net;
using JetBrains.Annotations;
using Microsoft.Win32;
using NanoByte.Common;
using NanoByte.Common.Native;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains control logic for applying <see cref="Store.Model.Capabilities.UrlProtocol"/> and <see cref="AccessPoints.UrlProtocol"/> on Windows systems.
    /// </summary>
    public static class UrlProtocol
    {
        #region Constants
        /// <summary>The registry value name used to indicate that a programmatic identifier is actually a ULR protocol handler.</summary>
        public const string ProtocolIndicator = "URL Protocol";

        /// <summary>The HKCU registry key where Windows Vista and newer store URL protocol associations.</summary>
        public const string RegKeyUserVistaUrlAssoc = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations";
        #endregion

        #region Register
        /// <summary>
        /// Registers a URL protocol in the current system.
        /// </summary>
        /// <param name="target">The application being integrated.</param>
        /// <param name="urlProtocol">The URL protocol to register.</param>
        /// <param name="machineWide">Register the URL protocol machine-wide instead of just for the current user.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <param name="accessPoint">Indicates that the handler shall become the default handler for the protocol.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurs while writing to the filesystem or registry.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="urlProtocol"/> is invalid.</exception>
        public static void Register(FeedTarget target, [NotNull] Store.Model.Capabilities.UrlProtocol urlProtocol, [NotNull] IIconStore iconStore, bool machineWide, bool accessPoint = false)
        {
            #region Sanity checks
            if (urlProtocol == null) throw new ArgumentNullException(nameof(urlProtocol));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            if (string.IsNullOrEmpty(urlProtocol.ID)) throw new InvalidDataException("Missing ID");

            var hive = machineWide ? Registry.LocalMachine : Registry.CurrentUser;

            if (urlProtocol.KnownPrefixes.Count == 0)
            {
                if (accessPoint)
                { // Can only be registered invasively by registering protocol ProgID (will replace existing and become default)
                    using (var progIDKey = hive.CreateSubKeyChecked(FileType.RegKeyClasses + @"\" + urlProtocol.ID))
                        FileType.RegisterVerbCapability(progIDKey, target, urlProtocol, iconStore, machineWide);
                }
            }
            else
            { // Can be registered non-invasively by registering custom ProgID (without becoming default)
                using (var progIDKey = hive.CreateSubKeyChecked(FileType.RegKeyClasses + @"\" + FileType.RegKeyPrefix + urlProtocol.ID))
                {
                    // Add flag to remember whether created for capability or access point
                    progIDKey.SetValue(accessPoint ? FileType.PurposeFlagAccessPoint : FileType.PurposeFlagCapability, "");

                    FileType.RegisterVerbCapability(progIDKey, target, urlProtocol, iconStore, machineWide);
                }

                if (accessPoint)
                {
                    foreach (var prefix in urlProtocol.KnownPrefixes)
                    {
                        if (WindowsUtils.IsWindowsVista && !machineWide)
                        {
                            using (var someKey = Registry.CurrentUser.CreateSubKeyChecked(RegKeyUserVistaUrlAssoc + @"\" + prefix.Value + @"\UserChoice"))
                                someKey.SetValue("ProgID", FileType.RegKeyPrefix + urlProtocol.ID);
                        }
                        else
                        {
                            // Setting default invasively by registering protocol ProgID
                            using (var progIDKey = hive.CreateSubKeyChecked(FileType.RegKeyClasses + @"\" + prefix.Value))
                                FileType.RegisterVerbCapability(progIDKey, target, urlProtocol, iconStore, machineWide);
                        }
                    }
                }
            }
        }
        #endregion

        #region Unregister
        /// <summary>
        /// Unregisters a URL protocol in the current system.
        /// </summary>
        /// <param name="urlProtocol">The URL protocol to remove.</param>
        /// <param name="machineWide">Unregister the URL protocol machine-wide instead of just for the current user.</param>
        /// <param name="accessPoint">Indicates that the handler was the default handler for the protocol.</param>
        /// <exception cref="IOException">A problem occurs while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="urlProtocol"/> is invalid.</exception>
        public static void Unregister([NotNull] Store.Model.Capabilities.UrlProtocol urlProtocol, bool machineWide, bool accessPoint = false)
        {
            #region Sanity checks
            if (urlProtocol == null) throw new ArgumentNullException(nameof(urlProtocol));
            #endregion

            if (string.IsNullOrEmpty(urlProtocol.ID)) throw new InvalidDataException("Missing ID");

            var hive = machineWide ? Registry.LocalMachine : Registry.CurrentUser;

            if (urlProtocol.KnownPrefixes.Count == 0)
            {
                if (accessPoint)
                { // Was registered invasively by registering protocol ProgID
                    try
                    {
                        hive.DeleteSubKeyTree(FileType.RegKeyClasses + @"\" + urlProtocol.ID);
                    }
                    #region Error handling
                    catch (ArgumentException)
                    {
                        // Ignore missing registry keys
                    }
                    #endregion
                }
            }
            else
            { // Was registered non-invasively by registering custom ProgID
                if (accessPoint)
                {
                    foreach (var prefix in urlProtocol.KnownPrefixes)
                    {
                        // TODO: Restore previous default
                    }
                }

                // Remove appropriate purpose flag and check if there are others
                bool otherFlags;
                using (var progIDKey = hive.OpenSubKey(FileType.RegKeyClasses + @"\" + FileType.RegKeyPrefix + urlProtocol.ID, writable: true))
                {
                    if (progIDKey == null) otherFlags = false;
                    else
                    {
                        progIDKey.DeleteValue(accessPoint ? FileType.PurposeFlagAccessPoint : FileType.PurposeFlagCapability, throwOnMissingValue: false);
                        otherFlags = progIDKey.GetValueNames().Any(name => name.StartsWith(FileType.PurposeFlagPrefix));
                    }
                }

                // Delete ProgID if there are no other references
                if (!otherFlags)
                {
                    try
                    {
                        hive.DeleteSubKeyTree(FileType.RegKeyClasses + @"\" + FileType.RegKeyPrefix + urlProtocol.ID);
                    }
                    #region Error handling
                    catch (ArgumentException)
                    {
                        // Ignore missing registry keys
                    }
                    #endregion
                }
            }
            #endregion
        }
    }
}
