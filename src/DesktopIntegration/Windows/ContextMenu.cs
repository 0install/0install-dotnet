// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using Microsoft.Win32;
using NanoByte.Common;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains control logic for applying <see cref="Store.Model.Capabilities.ContextMenu"/> and <see cref="AccessPoints.ContextMenu"/> on Windows systems.
    /// </summary>
    public static class ContextMenu
    {
        #region Constants
        /// <summary>Prepended before any verb name used by Zero Install in the registry. This prevents conflicts with non-Zero Install installations.</summary>
        internal const string RegKeyPrefix = "ZeroInstall.";

        /// <summary>The HKCU registry key for registering things for all files.</summary>
        public const string RegKeyClassesFiles = "*";

        /// <summary>The HKCU registry key for registering things for different kinds of executable files.</summary>
        public static readonly string[] RegKeyClassesExecutableFiles = {"exefile", "batfile", "cmdfile"};

        /// <summary>The HKCU registry key for registering things for all directories.</summary>
        public const string RegKeyClassesDirectories = "Directory";

        /// <summary>The HKCU registry key for registering things for all filesystem objects (files and directories).</summary>
        public const string RegKeyClassesAll = "AllFilesystemObjects";

        /// <summary>The registry key postfix for registering simple context menu entries.</summary>
        public const string RegKeyPostfix = @"shell";

        /// <summary>
        /// Gets the registry key name relevant for the specified context menu <paramref name="target"/>.
        /// </summary>
        private static IEnumerable<string> GetKeyName(Store.Model.Capabilities.ContextMenuTarget target)
            => target switch
            {
                Store.Model.Capabilities.ContextMenuTarget.Files => new[] {RegKeyClassesFiles},
                Store.Model.Capabilities.ContextMenuTarget.ExecutableFiles => RegKeyClassesExecutableFiles,
                Store.Model.Capabilities.ContextMenuTarget.Directories => new[] {RegKeyClassesDirectories},
                Store.Model.Capabilities.ContextMenuTarget.All => new[] {RegKeyClassesAll},
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
        /// <exception cref="IOException">A problem occurs while writing to the filesystem or registry.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="contextMenu"/> is invalid.</exception>
        public static void Apply(FeedTarget target, Store.Model.Capabilities.ContextMenu contextMenu, IIconStore iconStore, bool machineWide)
        {
            #region Sanity checks
            if (contextMenu == null) throw new ArgumentNullException(nameof(contextMenu));
            if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
            #endregion

            if (contextMenu.Verb == null) throw new InvalidDataException("Missing verb");
            if (string.IsNullOrEmpty(contextMenu.Verb.Name)) throw new InvalidDataException("Missing verb name");

            var hive = machineWide ? Registry.LocalMachine : Registry.CurrentUser;
            foreach (string keyName in GetKeyName(contextMenu.Target))
            {
                using var verbKey = hive.CreateSubKeyChecked(FileType.RegKeyClasses + @"\" + keyName + @"\shell\" + RegKeyPrefix + contextMenu.Verb.Name);
                string description = contextMenu.Verb.Descriptions.GetBestLanguage(CultureInfo.CurrentUICulture);
                if (description != null) verbKey.SetValue("", description);
                if (contextMenu.Verb.Extended) verbKey.SetValue(FileType.RegValueExtended, "");

                using var commandKey = verbKey.CreateSubKeyChecked("command");
                commandKey.SetValue("", FileType.GetLaunchCommandLine(target, contextMenu.Verb, iconStore, machineWide));
            }
        }
        #endregion

        #region Remove
        /// <summary>
        /// Removes a context menu entry from the current system.
        /// </summary>
        /// <param name="contextMenu">The context menu entry to remove.</param>
        /// <param name="machineWide">Remove the context menu entry machine-wide instead of just for the current user.</param>
        /// <exception cref="IOException">A problem occurs while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        /// <exception cref="InvalidDataException">The data in <paramref name="contextMenu"/> is invalid.</exception>
        public static void Remove(Store.Model.Capabilities.ContextMenu contextMenu, bool machineWide)
        {
            #region Sanity checks
            if (contextMenu == null) throw new ArgumentNullException(nameof(contextMenu));
            #endregion

            if (contextMenu.Verb == null) throw new InvalidDataException("Missing verb");
            if (string.IsNullOrEmpty(contextMenu.Verb.Name)) throw new InvalidDataException("Missing verb name");

            var hive = machineWide ? Registry.LocalMachine : Registry.CurrentUser;
            foreach (string keyName in GetKeyName(contextMenu.Target))
            {
                try
                {
                    hive.DeleteSubKeyTree(FileType.RegKeyClasses + @"\" + keyName + @"\shell\" + RegKeyPrefix + contextMenu.Verb.Name);
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
