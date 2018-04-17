// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using JetBrains.Annotations;
using Microsoft.Win32;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Capabilities;

namespace ZeroInstall.Publish.Capture
{
    partial class SnapshotDiff
    {
        /// <summary>
        /// Collects data about context menu entries.
        /// </summary>
        /// <param name="commandMapper">Provides best-match command-line to <see cref="Command"/> mapping.</param>
        /// <param name="capabilities">The capability list to add the collected data to.</param>
        /// <exception cref="IOException">There was an error accessing the registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
        public void CollectContextMenus([NotNull] CommandMapper commandMapper, [NotNull] CapabilityList capabilities)
        {
            #region Sanity checks
            if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));
            if (commandMapper == null) throw new ArgumentNullException(nameof(commandMapper));
            #endregion

            using (var progIDKey = Registry.ClassesRoot.OpenSubKey(DesktopIntegration.Windows.ContextMenu.RegKeyClassesFiles))
            {
                if (progIDKey == null) throw new IOException("Registry key not found");
                foreach (string entry in ContextMenuFiles)
                {
                    capabilities.Entries.Add(new ContextMenu
                    {
                        ID = "files-" + entry,
                        Target = ContextMenuTarget.Files,
                        Verb = GetVerb(progIDKey, commandMapper, entry)
                    });
                }
            }

            using (var progIDKey = Registry.ClassesRoot.OpenSubKey(DesktopIntegration.Windows.ContextMenu.RegKeyClassesExecutableFiles[0]))
            {
                if (progIDKey == null) throw new IOException("Registry key not found");
                foreach (string entry in ContextMenuExecutableFiles)
                {
                    capabilities.Entries.Add(new ContextMenu
                    {
                        ID = "executable-files-" + entry,
                        Target = ContextMenuTarget.ExecutableFiles,
                        Verb = GetVerb(progIDKey, commandMapper, entry)
                    });
                }
            }

            using (var progIDKey = Registry.ClassesRoot.OpenSubKey(DesktopIntegration.Windows.ContextMenu.RegKeyClassesDirectories))
            {
                if (progIDKey == null) throw new IOException("Registry key not found");
                foreach (string entry in ContextMenuDirectories)
                {
                    capabilities.Entries.Add(new ContextMenu
                    {
                        ID = "directories-" + entry,
                        Target = ContextMenuTarget.Directories,
                        Verb = GetVerb(progIDKey, commandMapper, entry)
                    });
                }
            }

            using (var progIDKey = Registry.ClassesRoot.OpenSubKey(DesktopIntegration.Windows.ContextMenu.RegKeyClassesAll))
            {
                if (progIDKey == null) throw new IOException("Registry key not found");
                foreach (string entry in ContextMenuAll)
                {
                    capabilities.Entries.Add(new ContextMenu
                    {
                        ID = "all-" + entry,
                        Target = ContextMenuTarget.Directories,
                        Verb = GetVerb(progIDKey, commandMapper, entry)
                    });
                }
            }
        }
    }
}
