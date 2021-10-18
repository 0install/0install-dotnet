// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Win32;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Model;
using ZeroInstall.Model.Capabilities;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains control logic for applying <see cref="Model.Capabilities.DefaultProgram"/> and <see cref="AccessPoints.DefaultProgram"/> on Windows systems.
    /// </summary>
    public static class DefaultProgram
    {
        #region Constants
        /// <summary>The HKLM registry key for registering applications as clients for specific services.</summary>
        public const string RegKeyMachineClients = @"SOFTWARE\Clients";

        /// <summary>The registry value name for localized name storage.</summary>
        public const string RegValueLocalizedName = "LocalizedString";

        /// <summary>The name of the registry subkeys containing information about application installation commands and status.</summary>
        public const string RegSubKeyInstallInfo = "InstallInfo";

        /// <summary>The registry value name below <see cref="RegSubKeyInstallInfo"/> for the command to set an application as the default program.</summary>
        public const string RegValueReinstallCommand = "ReinstallCommand";

        /// <summary>The registry value name below <see cref="RegSubKeyInstallInfo"/> for the command to create icons/shortcuts to the application.</summary>
        public const string RegValueShowIconsCommand = "ShowIconsCommand";

        /// <summary>The registry value name below <see cref="RegSubKeyInstallInfo"/> for the command to remove icons/shortcuts to the application.</summary>
        public const string RegValueHideIconsCommand = "HideIconsCommand";

        /// <summary>The registry value name below <see cref="RegSubKeyInstallInfo"/> for storing whether the application's icons are currently visible.</summary>
        public const string RegValueIconsVisible = "IconsVisible";
        #endregion

        #region Register
        /// <summary>
        /// Registers an application as a candidate for a default program for some service in the current system. This can only be applied machine-wide, not per user.
        /// </summary>
        /// <param name="target">The application being integrated.</param>
        /// <param name="defaultProgram">The default program information to be registered.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <param name="accessPoint">Indicates that the program should be set as the current default for the service it provides.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        public static void Register(FeedTarget target, Model.Capabilities.DefaultProgram defaultProgram, IIconStore iconStore, bool accessPoint = false)
        {
            #region Sanity checks
            if (defaultProgram == null) throw new ArgumentNullException(nameof(defaultProgram));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            using var serviceKey = Registry.LocalMachine.CreateSubKeyChecked($@"{RegKeyMachineClients}\{defaultProgram.Service}");
            using (var appKey = serviceKey.CreateSubKeyChecked(defaultProgram.ID))
            {
                appKey.SetValue("", target.Feed.Name);
                appKey.SetValue(accessPoint ? RegistryClasses.PurposeFlagAccessPoint : RegistryClasses.PurposeFlagCapability, "");
                RegistryClasses.Register(appKey, target, defaultProgram, iconStore, machineWide: true);

                // Set callbacks for Windows SPAD
                using (var installInfoKey = appKey.CreateSubKeyChecked(RegSubKeyInstallInfo))
                {
                    string exePath = Path.Combine(Locations.InstallBase, "0install-win.exe");
                    installInfoKey.SetValue(RegValueReinstallCommand, new[] {exePath, "integrate", "--machine", "--batch", "--add", "defaults", target.Uri.ToStringRfc()}.JoinEscapeArguments());
                    installInfoKey.SetValue(RegValueShowIconsCommand, new[] {exePath, "integrate", "--machine", "--batch", "--add", MenuEntry.TagName, "--add", DesktopIcon.TagName, target.Uri.ToStringRfc()}.JoinEscapeArguments());
                    installInfoKey.SetValue(RegValueHideIconsCommand, new[] {exePath, "integrate", "--machine", "--batch", "--remove", MenuEntry.TagName, "--remove", DesktopIcon.TagName, target.Uri.ToStringRfc()}.JoinEscapeArguments());
                    installInfoKey.SetValue(RegValueIconsVisible, 0, RegistryValueKind.DWord);
                }

                if (defaultProgram.Service == Model.Capabilities.DefaultProgram.ServiceMail)
                {
                    var mailToProtocol = new Model.Capabilities.UrlProtocol {Verbs = {new Verb {Name = Verb.NameOpen}}};
                    using var mailToKey = appKey.CreateSubKeyChecked(@"Protocols\mailto");
                    RegistryClasses.Register(mailToKey, target, mailToProtocol, iconStore, machineWide: true);
                }
            }

            if (accessPoint) serviceKey.SetValue("", defaultProgram.ID);
        }

        /// <summary>
        /// Toggles the registry entry indicating whether icons for the application are currently visible.
        /// </summary>
        /// <param name="defaultProgram">The default program information to be modified.</param>
        /// <param name="iconsVisible"><c>true</c> if the icons are currently visible, <c>false</c> if the icons are currently not visible.</param>
        internal static void ToggleIconsVisible(Model.Capabilities.DefaultProgram defaultProgram, bool iconsVisible)
        {
            using var installInfoKey = Registry.LocalMachine.CreateSubKeyChecked($@"{RegKeyMachineClients}\{defaultProgram.Service}\{defaultProgram.ID}\{RegSubKeyInstallInfo}");
            installInfoKey.SetValue(RegValueIconsVisible, iconsVisible ? 1 : 0, RegistryValueKind.DWord);
        }
        #endregion

        #region Unregister
        /// <summary>
        /// Unregisters an application as a candidate for a default program in the current system. This can only be applied machine-wide, not per user.
        /// </summary>
        /// <param name="defaultProgram">The default program information to be removed.</param>
        /// <param name="accessPoint">Indicates that the program was set as the current default for the service it provides.</param>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        public static void Unregister(Model.Capabilities.DefaultProgram defaultProgram, bool accessPoint = false)
        {
            #region Sanity checks
            if (defaultProgram == null) throw new ArgumentNullException(nameof(defaultProgram));
            #endregion

            using var serviceKey = Registry.LocalMachine.OpenSubKeyChecked($@"{RegKeyMachineClients}\{defaultProgram.Service}", writable: true);
            if (accessPoint)
            {
                // TODO: Restore previous default
            }

            // Remove appropriate purpose flag and check if there are others
            bool otherFlags;
            using (var appKey = serviceKey.OpenSubKey(defaultProgram.ID, writable: true))
            {
                if (appKey == null) otherFlags = false;
                else
                {
                    appKey.DeleteValue(accessPoint ? RegistryClasses.PurposeFlagAccessPoint : RegistryClasses.PurposeFlagCapability, throwOnMissingValue: false);
                    otherFlags = appKey.GetValueNames().Any(name => name.StartsWith(RegistryClasses.PurposeFlagPrefix));
                }
            }

            // Delete app key if there are no other references
            if (!otherFlags)
                serviceKey.DeleteSubKeyTree(defaultProgram.ID, throwOnMissingSubKey: false);
        }
        #endregion
    }
}
