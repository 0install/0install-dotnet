// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Versioning;
using Microsoft.Win32;
using NanoByte.Common.Native;
using ZeroInstall.Model;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Contains control logic for applying <see cref="Model.Capabilities.UrlProtocol"/> and <see cref="AccessPoints.UrlProtocol"/> on Windows systems.
/// </summary>
[SupportedOSPlatform("windows")]
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
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    public static void Register(FeedTarget target, Model.Capabilities.UrlProtocol urlProtocol, IIconStore iconStore, bool machineWide, bool accessPoint = false)
    {
        #region Sanity checks
        if (urlProtocol == null) throw new ArgumentNullException(nameof(urlProtocol));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        using var classesKey = RegistryClasses.OpenHive(machineWide);

        if (urlProtocol.KnownPrefixes.Count == 0)
        {
            if (accessPoint)
            { // Can only be registered invasively by registering protocol ProgID (will replace existing and become default)
                using var progIDKey = classesKey.CreateSubKeyChecked(urlProtocol.ID);
                progIDKey.SetValue("", urlProtocol.Descriptions.GetBestLanguage(CultureInfo.CurrentUICulture) ?? urlProtocol.ID);
                RegistryClasses.Register(progIDKey, target, urlProtocol, iconStore, machineWide);
                progIDKey.SetValue(ProtocolIndicator, "");
            }
        }
        else
        { // Can be registered non-invasively by registering custom ProgID (without becoming default)
            using (var progIDKey = classesKey.CreateSubKeyChecked(RegistryClasses.Prefix + urlProtocol.ID))
            {
                progIDKey.SetValue("", urlProtocol.Descriptions.GetBestLanguage(CultureInfo.CurrentUICulture) ?? urlProtocol.ID);
                progIDKey.SetValue(accessPoint ? RegistryClasses.PurposeFlagAccessPoint : RegistryClasses.PurposeFlagCapability, "");
                RegistryClasses.Register(progIDKey, target, urlProtocol, iconStore, machineWide);
                progIDKey.SetValue(ProtocolIndicator, "");
            }

            if (accessPoint)
            {
                foreach (var prefix in urlProtocol.KnownPrefixes)
                {
                    if (WindowsUtils.IsWindowsVista && !machineWide)
                    {
                        using var userChoiceKey = Registry.CurrentUser.CreateSubKeyChecked($@"{RegKeyUserVistaUrlAssoc}\{prefix.Value}\UserChoice");
                        userChoiceKey.SetValue("ProgID", RegistryClasses.Prefix + urlProtocol.ID);
                    }
                    else
                    {
                        // Setting default invasively by registering protocol ProgID
                        using var progIDKey = classesKey.CreateSubKeyChecked(prefix.Value);
                        RegistryClasses.Register(progIDKey, target, urlProtocol, iconStore, machineWide);
                        progIDKey.SetValue(ProtocolIndicator, "");
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
    /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    public static void Unregister(Model.Capabilities.UrlProtocol urlProtocol, bool machineWide, bool accessPoint = false)
    {
        #region Sanity checks
        if (urlProtocol == null) throw new ArgumentNullException(nameof(urlProtocol));
        #endregion

        using var classesKey = RegistryClasses.OpenHive(machineWide);

        if (urlProtocol.KnownPrefixes.Count == 0)
        {
            if (accessPoint) // Was registered invasively by registering protocol ProgID
                classesKey.DeleteSubKeyTree(urlProtocol.ID, throwOnMissingSubKey: false);
        }
        else
        { // Was registered non-invasively by registering custom ProgID
            if (accessPoint)
            {
                // TODO: Restore previous default
                // foreach (var prefix in urlProtocol.KnownPrefixes)
                // {
                // }
            }

            // Remove appropriate purpose flag and check if there are others
            bool otherFlags;
            using (var progIDKey = classesKey.OpenSubKey(RegistryClasses.Prefix + urlProtocol.ID, writable: true))
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
                classesKey.DeleteSubKeyTree(RegistryClasses.Prefix + urlProtocol.ID, throwOnMissingSubKey: false);
        }
        #endregion
    }
}
