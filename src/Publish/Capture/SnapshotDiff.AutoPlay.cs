// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using NanoByte.Common.Collections;
using ZeroInstall.Model;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.Publish.Capture
{
    partial class SnapshotDiff
    {
        /// <summary>
        /// Collects data about AutoPlay handlers.
        /// </summary>
        /// <param name="commandMapper">Provides best-match command-line to <see cref="Command"/> mapping.</param>
        /// <param name="capabilities">The capability list to add the collected data to.</param>
        /// <exception cref="IOException">There was an error accessing the registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
        public void CollectAutoPlays(CommandMapper commandMapper, CapabilityList capabilities)
        {
            #region Sanity checks
            if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));
            if (commandMapper == null) throw new ArgumentNullException(nameof(commandMapper));
            #endregion

            capabilities.Entries.AddRange(AutoPlayHandlersUser
                .Select(handler => GetAutoPlay(handler, Registry.CurrentUser, AutoPlayAssocsUser, commandMapper))
                .WhereNotNull());

            capabilities.Entries.AddRange(AutoPlayHandlersMachine
                .Select(handler => GetAutoPlay(handler, Registry.LocalMachine, AutoPlayAssocsMachine, commandMapper))
                .WhereNotNull());
        }

        /// <summary>
        /// Retrieves data about a AutoPlay handler type from a snapshot diff.
        /// </summary>
        /// <param name="handler">The internal name of the AutoPlay handler.</param>
        /// <param name="hive">The registry hive to search in (usually HKCU or HKLM).</param>
        /// <param name="autoPlayAssocs">A list of associations of an AutoPlay events with an AutoPlay handlers</param>
        /// <param name="commandMapper">Provides best-match command-line to <see cref="Command"/> mapping.</param>
        /// <exception cref="IOException">There was an error accessing the registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
        private static Capability? GetAutoPlay(string handler, RegistryKey hive, IEnumerable<ComparableTuple<string>> autoPlayAssocs, CommandMapper commandMapper)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (hive == null) throw new ArgumentNullException(nameof(hive));
            if (autoPlayAssocs == null) throw new ArgumentNullException(nameof(autoPlayAssocs));
            if (commandMapper == null) throw new ArgumentNullException(nameof(commandMapper));
            #endregion

            using var handlerKey = hive.OpenSubKey(DesktopIntegration.Windows.AutoPlay.RegKeyHandlers + @"\" + handler);
            string progID = handlerKey?.GetValue(DesktopIntegration.Windows.AutoPlay.RegValueProgID)?.ToString();
            if (string.IsNullOrEmpty(progID)) return null;

            string verbName = handlerKey.GetValue(DesktopIntegration.Windows.AutoPlay.RegValueVerb)?.ToString();
            if (string.IsNullOrEmpty(verbName)) return null;

            using var progIDKey = Registry.ClassesRoot.OpenSubKey(progID);
            if (progIDKey == null) throw new IOException(progID + " key not found");
            var autoPlay = new AutoPlay
            {
                ID = handler,
                Provider = handlerKey.GetValue(DesktopIntegration.Windows.AutoPlay.RegValueProvider)?.ToString(),
                Descriptions = {handlerKey.GetValue(DesktopIntegration.Windows.AutoPlay.RegValueDescription)?.ToString()},
                Verb = GetVerb(progIDKey, commandMapper, verbName)
            };

            autoPlay.Events.AddRange(
                from autoPlayAssoc in autoPlayAssocs
                where autoPlayAssoc.Value == handler
                select new AutoPlayEvent {Name = autoPlayAssoc.Key});

            return autoPlay;
        }
    }
}
