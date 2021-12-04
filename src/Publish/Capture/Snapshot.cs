// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Win32;
using NanoByte.Common;
using NanoByte.Common.Native;
using ZeroInstall.DesktopIntegration.Windows;
using ZeroInstall.Publish.Properties;

namespace ZeroInstall.Publish.Capture
{
    /// <summary>
    /// Represents the systems state at a point in time. This is used to determine changes.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [Serializable]
    public class Snapshot
    {
        /// <summary>A list of associations of services with clients (e.g. web browsers, mail readers, ...).</summary>
        public List<(string name, string client)> ServiceAssocs { get; } = new();

        /// <summary>A list of applications registered as AutoPlay handlers.</summary>
        public List<string> AutoPlayHandlersUser { get; } = new();

        /// <summary>A list of applications registered as AutoPlay handlers.</summary>
        public List<string> AutoPlayHandlersMachine { get; } = new();

        /// <summary>A list of associations of AutoPlay events with AutoPlay handlers.</summary>
        public List<(string name, string handler)> AutoPlayAssocsUser { get; } = new();

        /// <summary>A list of associations of AutoPlay events with AutoPlay handlers.</summary>
        public List<(string name, string handler)> AutoPlayAssocsMachine { get; } = new();

        /// <summary>A list of associations of file extensions with programmatic identifiers.</summary>
        public List<(string extension, string progID)> FileAssocs { get; } = new();

        /// <summary>A list of protocol associations for well-known protocols (e.g. HTTP, FTP, ...).</summary>
        public List<(string protocol, string progID)> ProtocolAssocs { get; } = new();

        /// <summary>A list of programmatic identifiers.</summary>
        public List<string> ProgIDs { get; } = new();

        /// <summary>A list of COM class IDs.</summary>
        public List<string> ClassIDs { get; } = new();

        /// <summary>A list of applications registered as candidates for default programs.</summary>
        public List<string> RegisteredApplications { get; } = new();

        /// <summary>A list of context menu entries for all files.</summary>
        public List<string> ContextMenuFiles { get; } = new();

        /// <summary>A list of context menu entries for executable files.</summary>
        public List<string> ContextMenuExecutableFiles { get; } = new();

        /// <summary>A list of context menu entries for all directories.</summary>
        public List<string> ContextMenuDirectories { get; } = new();

        /// <summary>A list of context menu entries for all filesystem objects (files and directories).</summary>
        public List<string> ContextMenuAll { get; } = new();

        /// <summary>A list of program installation directories.</summary>
        public List<string> ProgramsDirs { get; } = new();

        /// <summary>
        /// Takes a snapshot of the current system state.
        /// </summary>
        /// <returns>The newly created snapshot.</returns>
        /// <exception cref="IOException">There was an error accessing the registry or file system.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the registry or file system was not permitted.</exception>
        /// <exception cref="PlatformNotSupportedException">This method is called while running on a platform for which capturing is not supported.</exception>
        public static Snapshot Take()
        {
            if (!WindowsUtils.IsWindows) throw new PlatformNotSupportedException(Resources.OnlyAvailableOnWindows);

            var snapshot = new Snapshot();
            snapshot.TakeRegistry();
            snapshot.TakeFileSystem();
            return snapshot;
        }

        #region Registry
        /// <summary>
        /// Stores information about the current state of the registry in a snapshot.
        /// </summary>
        /// <exception cref="IOException">There was an error accessing the registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
        private void TakeRegistry()
        {
            ServiceAssocs.AddRange(GetServiceAssocs());
            AutoPlayHandlersUser.AddRange(RegUtils.GetSubKeyNames(Registry.CurrentUser, AutoPlay.RegKeyHandlers));
            AutoPlayHandlersMachine.AddRange(RegUtils.GetSubKeyNames(Registry.LocalMachine, AutoPlay.RegKeyHandlers));
            AutoPlayAssocsUser.AddRange(GetAutoPlayAssocs(Registry.CurrentUser));
            AutoPlayAssocsMachine.AddRange(GetAutoPlayAssocs(Registry.LocalMachine));

            var (fileAssocs, progIDs) = GetFileAssocData();
            FileAssocs.AddRange(fileAssocs);
            ProgIDs.AddRange(progIDs);

            ProtocolAssocs.AddRange(GetProtocolAssoc());
            ClassIDs.AddRange(RegUtils.GetSubKeyNames(Registry.ClassesRoot, ComServer.RegKeyClassesIDs));
            RegisteredApplications.AddRange(RegUtils.GetValueNames(Registry.LocalMachine, AppRegistration.RegKeyMachineRegisteredApplications));

            ContextMenuFiles.AddRange(RegUtils.GetSubKeyNames(Registry.ClassesRoot, ContextMenu.RegKeyClassesFiles + @"\shell"));
            ContextMenuExecutableFiles.AddRange(RegUtils.GetSubKeyNames(Registry.ClassesRoot, ContextMenu.RegKeyClassesExecutableFiles + @"\shell"));
            ContextMenuDirectories.AddRange(RegUtils.GetSubKeyNames(Registry.ClassesRoot, ContextMenu.RegKeyClassesDirectories + @"\shell"));
            ContextMenuAll.AddRange(RegUtils.GetSubKeyNames(Registry.ClassesRoot, ContextMenu.RegKeyClassesAll + @"\shell"));
        }

        /// <summary>
        /// Retrieves a list of service associations from the registry.
        /// </summary>
        /// <exception cref="IOException">There was an error accessing the registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
        private static IReadOnlyCollection<(string serviceName, string clientName)> GetServiceAssocs()
        {
            using var clientsKey = Registry.LocalMachine.OpenSubKey(DefaultProgram.RegKeyMachineClients);
            if (clientsKey == null) return Array.Empty<(string, string)>();

            return (
                from serviceName in clientsKey.GetSubKeyNames()
                from clientName in RegUtils.GetSubKeyNames(clientsKey, serviceName)
                select (serviceName, clientName)
            ).ToList();
        }

        /// <summary>
        /// Retrieves a list of file associations and programmatic identifiers the registry.
        /// </summary>
        private static (IReadOnlyCollection<(string name, string handler)> fileAssocs, IReadOnlyCollection<string> progIDs) GetFileAssocData()
        {
            var fileAssocsList = new List<(string name, string handler)>();
            var progIDsList = new List<string>();

            foreach (string keyName in Registry.ClassesRoot.GetSubKeyNames())
            {
                if (keyName.StartsWith("."))
                {
                    using var assocKey = Registry.ClassesRoot.OpenSubKey(keyName);
                    if (assocKey == null) continue;

                    // Get the main ProgID
                    string? assocValue = assocKey.GetValue("")?.ToString();
                    if (string.IsNullOrEmpty(assocValue)) continue;
                    fileAssocsList.Add((keyName, assocValue));

                    // Get additional ProgIDs
                    fileAssocsList.AddRange(RegUtils.GetValueNames(assocKey, FileType.RegSubKeyOpenWith).Select(progID => (keyName, progID)));
                }
                else progIDsList.Add(keyName);
            }

            return (fileAssocsList, progIDsList);
        }

        /// <summary>
        /// Retrieves a list of protocol associations for well-known protocols (e.g. HTTP, FTP, ...).
        /// </summary>
        private static IEnumerable<(string protocol, string command)> GetProtocolAssoc()
        {
            foreach (string protocol in new[] { "ftp", "gopher", "http", "https" })
            {
                string? command = RegistryUtils.GetString(@"HKEY_CLASSES_ROOT\" + protocol + @"\shell\open\command", valueName: null);
                if (!string.IsNullOrEmpty(command))
                    yield return (protocol, command);
            }
        }

        /// <summary>
        /// Retrieves a list of AutoPlay associations from the registry.
        /// </summary>
        /// <param name="hive">The registry hive to search in (usually HKCU or HKLM).</param>
        /// <exception cref="IOException">There was an error accessing the registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
        private static IReadOnlyCollection<(string eventName, string handlerName)> GetAutoPlayAssocs(RegistryKey hive)
        {
            using var eventsKey = hive.OpenSubKey(AutoPlay.RegKeyAssocs);
            if (eventsKey == null) return Array.Empty<(string, string)>();

            return (
                from eventName in eventsKey.GetSubKeyNames()
                from handlerName in RegUtils.GetValueNames(eventsKey, eventName)
                select (eventName, handlerName)
            ).ToList();
        }
        #endregion

        #region File system
        /// <summary>
        /// Stores information about the current state of the file system in a snapshot.
        /// </summary>
        /// <exception cref="IOException">There was an error accessing the file system.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the file system was not permitted.</exception>
        private void TakeFileSystem()
        {
            // Locate installation directories
            string? programFiles32Bit = Environment.Is64BitProcess
                ? Environment.GetEnvironmentVariable("ProgramFiles(x86)")
                : Environment.GetEnvironmentVariable("ProgramFiles");
            string? programFiles64Bit = Environment.Is64BitProcess
                ? Environment.GetEnvironmentVariable("ProgramFiles")
                : null;

            // Build a list of all installation directories
            if (string.IsNullOrEmpty(programFiles32Bit)) Log.Warn(Resources.MissingProgramFiles32Bit);
            else ProgramsDirs.AddRange(Directory.GetDirectories(programFiles32Bit));
            if (!string.IsNullOrEmpty(programFiles64Bit))
                ProgramsDirs.AddRange(Directory.GetDirectories(programFiles64Bit));
        }
        #endregion
    }
}
