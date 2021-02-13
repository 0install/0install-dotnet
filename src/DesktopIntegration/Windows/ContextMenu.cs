// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using NanoByte.Common.Native;
using ZeroInstall.Model;
using ZeroInstall.Model.Capabilities;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains control logic for applying <see cref="Model.Capabilities.ContextMenu"/> and <see cref="AccessPoints.ContextMenu"/> on Windows systems.
    /// </summary>
    public static class ContextMenu
    {
        #region Constants
        /// <summary>Prepended before programmatic identifiers used by Zero Install in the registry. This prevents conflicts with non-Zero Install installations.</summary>
        public const string Prefix = RegistryClasses.Prefix + "ContextMenu.";

        /// <summary>The HKCU registry key for registering things for all files.</summary>
        public const string RegKeyClassesFiles = "*";

        /// <summary>The HKCU registry key for registering things for different kinds of executable files.</summary>
        public static readonly string[] RegKeyClassesExecutableFiles = {"exefile", "batfile", "cmdfile"};

        /// <summary>The HKCU registry key for registering things for all directories.</summary>
        public const string RegKeyClassesDirectories = "Directory";

        /// <summary>The HKCU registry key for registering things for all filesystem objects (files and directories).</summary>
        public const string RegKeyClassesAll = "AllFilesystemObjects";

        /// <summary>
        /// Gets the registry key name relevant for the specified context menu <paramref name="target"/>.
        /// </summary>
        private static IEnumerable<string> GetKeyName(ContextMenuTarget target)
            => target switch
            {
                ContextMenuTarget.Files => new[] {RegKeyClassesFiles},
                ContextMenuTarget.ExecutableFiles => RegKeyClassesExecutableFiles,
                ContextMenuTarget.Directories => new[] {RegKeyClassesDirectories, RegKeyClassesDirectories + @"\Background"},
                ContextMenuTarget.All => new[] {RegKeyClassesAll},
                _ => new[] {RegKeyClassesFiles}
            };
        #endregion

        #region Apply
        /// <summary>
        /// Adds a context menu entry to the current system.
        /// </summary>
        /// <param name="target">The application being integrated.</param>
        /// <param name="contextMenu">The context menu entry to add.</param>
        /// <param name="iconStore">A callback object used when the the user is to be informed about the progress of long-running operations such as downloads.</param>
        /// <param name="machineWide">Add the context menu entry machine-wide instead of just for the current user.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="contextMenu"/> is invalid.</exception>
        public static void Apply(FeedTarget target, Model.Capabilities.ContextMenu contextMenu, IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (contextMenu == null) throw new ArgumentNullException(nameof(contextMenu));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            if (string.IsNullOrEmpty(contextMenu.ID)) throw new InvalidDataException("Missing ID");

            using var classesKey = RegistryClasses.OpenHive(machineWide);

            if (contextMenu.Verbs.Count == 1)
            { // Simple context menu entry
                var verb = contextMenu.Verbs.Single();
                if (string.IsNullOrEmpty(verb.Name)) throw new InvalidDataException("Missing verb name");

                foreach (string keyName in GetKeyName(contextMenu.Target))
                {
                    using var verbKey = classesKey.CreateSubKeyChecked($@"{keyName}\shell\{RegistryClasses.Prefix}{verb.Name}");
                    RegistryClasses.Register(verbKey, target, verb, iconStore, machineWide);

                    var icon = contextMenu.GetIcon(Icon.MimeTypeIco);
                    if (icon != null)
                        verbKey.SetValue("Icon", iconStore.GetPath(icon));
                }
            }
            else
            { // Cascading context menu
                using (var menuKey = classesKey.CreateSubKeyChecked(Prefix + contextMenu.ID))
                    RegistryClasses.Register(menuKey, target, contextMenu, iconStore, machineWide);

                foreach (string keyName in GetKeyName(contextMenu.Target))
                {
                    using var menuKey = classesKey.CreateSubKeyChecked($@"{keyName}\shell\{RegistryClasses.Prefix}{contextMenu.ID}");

                    menuKey.SetValue("ExtendedSubCommandsKey", Prefix + contextMenu.ID);
                    menuKey.SetValue("MUIVerb", contextMenu.Descriptions.GetBestLanguage(CultureInfo.CurrentUICulture) ?? contextMenu.ID);

                    var icon = contextMenu.GetIcon(Icon.MimeTypeIco) ?? target.Feed.Icons.GetIcon(Icon.MimeTypeIco);
                    if (icon != null) menuKey.SetValue("Icon", iconStore.GetPath(icon));
                }
            }
        }
        #endregion

        #region Remove
        /// <summary>
        /// Removes a context menu entry from the current system.
        /// </summary>
        /// <param name="contextMenu">The context menu entry to remove.</param>
        /// <param name="machineWide">Remove the context menu entry machine-wide instead of just for the current user.</param>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="contextMenu"/> is invalid.</exception>
        public static void Remove(Model.Capabilities.ContextMenu contextMenu, bool machineWide)
        {
            #region Sanity checks
            if (contextMenu == null) throw new ArgumentNullException(nameof(contextMenu));
            #endregion

            if (string.IsNullOrEmpty(contextMenu.ID)) throw new InvalidDataException("Missing ID");

            using var classesKey = RegistryClasses.OpenHive(machineWide);

            if (contextMenu.Verbs.Count == 1)
            { // Simple context menu entry
                foreach (string keyName in GetKeyName(contextMenu.Target))
                    classesKey.DeleteSubKeyTree($@"{keyName}\shell\{RegistryClasses.Prefix}{contextMenu.Verbs.Single().Name}", throwOnMissingSubKey: false);
            }
            else
            { // Cascading context menu
                classesKey.DeleteSubKeyTree(Prefix + contextMenu.ID, throwOnMissingSubKey: false);

                foreach (string keyName in GetKeyName(contextMenu.Target))
                    classesKey.DeleteSubKeyTree($@"{keyName}\shell\{RegistryClasses.Prefix}{contextMenu.ID}", throwOnMissingSubKey: false);
            }
        }
        #endregion
    }
}
