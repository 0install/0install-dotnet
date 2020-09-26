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
using ZeroInstall.Model;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains control logic for applying <see cref="Model.Capabilities.AutoPlay"/> and <see cref="AccessPoints.AutoPlay"/> on Windows systems.
    /// </summary>
    public static class AutoPlay
    {
        #region Constants
        /// <summary>The HKCU/HKLM registry key for storing AutoPlay handlers.</summary>
        public const string RegKeyHandlers = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\Handlers";

        /// <summary>The HKCU/HKLM registry key for storing AutoPlay handler associations.</summary>
        public const string RegKeyAssocs = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlers";

        /// <summary>The HKCU registry key for storing user-selected AutoPlay handlers.</summary>
        public const string RegKeyChosenAssocs = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers";

        /// <summary>The registry value name for storing the programmatic identifier to invoke.</summary>
        public const string RegValueProgID = "InvokeProgID";

        /// <summary>The registry value name for storing the name of the verb to invoke.</summary>
        public const string RegValueVerb = "InvokeVerb";

        /// <summary>The registry value name for storing the name of the application providing the AutoPlay action.</summary>
        public const string RegValueProvider = "Provider";

        /// <summary>The registry value name for storing the description of the AutoPlay action.</summary>
        public const string RegValueDescription = "Action";

        /// <summary>The registry value name for storing the icon for the AutoPlay action.</summary>
        public const string RegValueIcon = "DefaultIcon";
        #endregion

        #region Register
        /// <summary>
        /// Adds an AutoPlay handler registration to the current system.
        /// </summary>
        /// <param name="target">The application being integrated.</param>
        /// <param name="autoPlay">The AutoPlay handler information to be applied.</param>
        /// <param name="machineWide">Register the handler machine-wide instead of just for the current user.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <param name="accessPoint">Indicates that the handler should become the default handler for all <see cref="Model.Capabilities.AutoPlay.Events"/>.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="autoPlay"/> is invalid.</exception>
        public static void Register(FeedTarget target, Model.Capabilities.AutoPlay autoPlay, IIconStore iconStore, bool machineWide, bool accessPoint = false)
        {
            #region Sanity checks
            if (autoPlay == null) throw new ArgumentNullException(nameof(autoPlay));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            if (string.IsNullOrEmpty(autoPlay.ID)) throw new InvalidDataException("Missing ID");
            if (string.IsNullOrEmpty(autoPlay.Provider)) throw new InvalidDataException("Missing provider");

            var verb = autoPlay.Verb;
            if (verb == null) throw new InvalidDataException("Missing verb");
            if (string.IsNullOrEmpty(verb.Name)) throw new InvalidDataException("Missing verb name");

            string handlerName = RegistryClasses.Prefix + autoPlay.ID;
            string progId = RegistryClasses.Prefix + "AutoPlay." + autoPlay.ID;

            using (var classesKey = RegistryClasses.OpenHive(machineWide))
            using (var verbKey = classesKey.CreateSubKeyChecked($@"{progId}\shell\{verb.Name}"))
                RegistryClasses.Register(verbKey, target, verb, iconStore, machineWide);

            var hive = machineWide ? Registry.LocalMachine : Registry.CurrentUser;

            using (var handlerKey = hive.CreateSubKeyChecked($@"{RegKeyHandlers}\{handlerName}"))
            {
                handlerKey.SetValue(accessPoint ? RegistryClasses.PurposeFlagAccessPoint : RegistryClasses.PurposeFlagCapability, "");
                handlerKey.SetValue(RegValueProgID, progId);
                handlerKey.SetValue(RegValueVerb, verb.Name);
                handlerKey.SetValue(RegValueProvider, autoPlay.Provider);
                handlerKey.SetValue(RegValueDescription, autoPlay.Descriptions.GetBestLanguage(CultureInfo.CurrentUICulture) ?? verb.Name);

                var icon = autoPlay.GetIcon(Icon.MimeTypeIco) ?? target.Feed.GetBestIcon(Icon.MimeTypeIco, verb.Command);
                if (icon != null)
                    handlerKey.SetValue(RegValueIcon, iconStore.GetPath(icon) + ",0");
            }

            foreach (var autoPlayEvent in autoPlay.Events.Except(x => string.IsNullOrEmpty(x.Name)))
            {
                using (var eventKey = hive.CreateSubKeyChecked($@"{RegKeyAssocs}\{autoPlayEvent.Name}"))
                    eventKey.SetValue(handlerName, "");

                if (accessPoint)
                {
                    using var chosenEventKey = hive.CreateSubKeyChecked($@"{RegKeyChosenAssocs}\{autoPlayEvent.Name}");
                    chosenEventKey.SetValue("", handlerName);
                }
            }
        }
        #endregion

        #region Unregister
        /// <summary>
        /// Removes an AutoPlay handler registration from the current system.
        /// </summary>
        /// <param name="autoPlay">The AutoPlay handler information to be removed.</param>
        /// <param name="machineWide">Remove the handler machine-wide instead of just for the current user.</param>
        /// <param name="accessPoint">Indicates that the handler should was the default handler for all <see cref="Model.Capabilities.AutoPlay.Events"/>.</param>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="autoPlay"/> is invalid.</exception>
        public static void Unregister(Model.Capabilities.AutoPlay autoPlay, bool machineWide, bool accessPoint = false)
        {
            #region Sanity checks
            if (autoPlay == null) throw new ArgumentNullException(nameof(autoPlay));
            #endregion

            if (string.IsNullOrEmpty(autoPlay.ID)) throw new InvalidDataException("Missing ID");

            var hive = machineWide ? Registry.LocalMachine : Registry.CurrentUser;
            string handlerName = RegistryClasses.Prefix + autoPlay.ID;
            string progId = RegistryClasses.Prefix + "AutoPlay." + autoPlay.ID;

            if (accessPoint)
            {
                // TODO: Restore previous default
            }

            // Remove appropriate purpose flag and check if there are others
            bool otherFlags;
            using (var handlerKey = hive.OpenSubKey($@"{RegKeyHandlers}\{handlerName}", writable: true))
            {
                if (handlerKey == null) otherFlags = false;
                else
                {
                    handlerKey.DeleteValue(accessPoint ? RegistryClasses.PurposeFlagAccessPoint : RegistryClasses.PurposeFlagCapability, throwOnMissingValue: false);
                    otherFlags = handlerKey.GetValueNames().Any(name => name.StartsWith(RegistryClasses.PurposeFlagPrefix));
                }
            }

            // Delete handler key and ProgID if there are no other references
            if (!otherFlags)
            {
                foreach (var autoPlayEvent in autoPlay.Events.Except(x => string.IsNullOrEmpty(x.Name)))
                {
                    using var eventKey = hive.OpenSubKey($@"{RegKeyAssocs}\{autoPlayEvent.Name}", writable: true);
                    eventKey?.DeleteValue(handlerName, throwOnMissingValue: false);
                }

                hive.DeleteSubKeyTree($@"{RegKeyHandlers}\{handlerName}", throwOnMissingSubKey: false);

                using var classesKey = RegistryClasses.OpenHive(machineWide);
                classesKey.DeleteSubKeyTree(progId, throwOnMissingSubKey: false);
            }
        }
        #endregion
    }
}
