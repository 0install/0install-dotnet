// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using System.Security;
using Microsoft.Win32;
using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Contains control logic for applying <see cref="Model.Capabilities.FileType"/> and <see cref="AccessPoints.FileType"/> on Windows systems.
/// </summary>
[SupportedOSPlatform("windows")]
public static partial class FileType
{
    #region Constants
    /// <summary>The registry value name for friendly type name storage.</summary>
    public const string RegValueFriendlyName = "FriendlyTypeName";

    /// <summary>The registry value name for MIME type storage.</summary>
    public const string RegValueContentType = "Content Type";

    /// <summary>The registry value name for perceived type storage.</summary>
    public const string RegValuePerceivedType = "PerceivedType";

    /// <summary>The registry subkey containing "open with" ProgID references.</summary>
    public const string RegSubKeyOpenWith = "OpenWithProgIDs";

    /// <summary>The registry subkey below HKEY_CLASSES_ROOT that contains MIME type to extension mapping.</summary>
    private const string RegSubKeyMimeType = @"MIME\Database\Content Type";

    /// <summary>The registry value name for a MIME type extension association.</summary>
    private const string RegValueExtension = "Extension";

    /// <summary>The registry key containing Windows Explorer file extension settings.</summary>
    private const string RegKeyExplorerFileExtensions = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts";
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
    public static void Register(FeedTarget target, Model.Capabilities.FileType fileType, IIconStore iconStore, bool machineWide, bool accessPoint = false)
    {
        #region Sanity checks
        if (fileType == null) throw new ArgumentNullException(nameof(fileType));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        using var classesKey = RegistryClasses.OpenHive(machineWide);

        // Register ProgID
        string progID = RegistryClasses.Prefix + fileType.ID;
        using (var progIDKey = classesKey.CreateSubKeyChecked(progID))
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
                extensionKey.SetOrDelete(RegValueContentType, extension.MimeType);
                extensionKey.SetOrDelete(RegValuePerceivedType, extension.PerceivedType);

                using (var openWithKey = extensionKey.CreateSubKeyChecked(RegSubKeyOpenWith))
                    openWithKey.SetValue(progID, "");

                if (accessPoint)
                {
                    if (!machineWide && WindowsUtils.IsWindowsVista) SetUserChoice(extension, progID);
                    else extensionKey.SetValue("", progID);
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

    private static void SetUserChoice(FileTypeExtension extension, string progID)
    {
        using var extensionsKey = Registry.CurrentUser.OpenSubKeyChecked(RegKeyExplorerFileExtensions, writable: true);
        using var extensionKey = extensionsKey.CreateSubKeyChecked(extension.Value);

        using (var userChoiceKey = extensionKey.TryOpenSubKey("UserChoice", writable: false))
        {
            // Leave unchanged if user choice already points to the desired value
            if ((userChoiceKey?.GetValue("Progid") ?? "").ToString() == progID) return;
        }

        try
        {
            // Must delete and recreate instead of direct modification due to ACLs
            extensionKey.DeleteSubKey("UserChoice", throwOnMissingSubKey: false);

            using var userChoiceKey = extensionKey.CreateSubKeyChecked("UserChoice");
            userChoiceKey.SetValue("Progid", progID);
            userChoiceKey.SetValue("Hash", CalculateHash(extension.Value, progID, userChoiceKey.GetLastWriteTime()));
        }
        #region Error handling
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            Log.Info("Failed to modify file type association user choice", ex);
        }
        #endregion
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
    public static void Unregister(Model.Capabilities.FileType fileType, bool machineWide, bool accessPoint = false)
    {
        #region Sanity checks
        if (fileType == null) throw new ArgumentNullException(nameof(fileType));
        #endregion

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
            //         using (var overridesKey = hive.TryOpenSubKey(RegKeyOverrides, true))
            //         using (var extensionOverrideKey = overridesKey.CreateSubKeyChecked(extension.Value))
            //         {
            //             // Must delete and recreate instead of direct modification due to wicked ACLs
            //             extensionOverrideKey.TryDeleteSubKey("UserChoice");
            //             using (var userChoiceKey = extensionOverrideKey.CreateSubKeyChecked("UserChoice"))
            //                 userChoiceKey.SetValue("ProgID", fileType.PreviousID);
            //         }
            //     }
            // }

            // Unregister extensions
            using var extensionKey = classesKey.TryOpenSubKey(extension.Value, writable: true);
            if (extensionKey != null)
            {
                using var openWithKey = extensionKey.TryOpenSubKey(RegSubKeyOpenWith, writable: true);
                openWithKey?.DeleteValue(RegistryClasses.Prefix + fileType.ID, throwOnMissingValue: false);
            }

            if (accessPoint)
            {
                // TODO: Restore previous default
            }
        }

        // Remove appropriate purpose flag and check if there are others
        bool otherFlags;
        using (var progIDKey = classesKey.TryOpenSubKey(RegistryClasses.Prefix + fileType.ID, writable: true))
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
            classesKey.TryDeleteSubKey(RegistryClasses.Prefix + fileType.ID);
    }
    #endregion
}
