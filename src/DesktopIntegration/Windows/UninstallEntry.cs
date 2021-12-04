// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Versioning;
using Microsoft.Win32;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using ZeroInstall.Model;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Manages uninstall registry entries on Windows systems.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class UninstallEntry
    {
        /// <summary>
        /// Adds an entry to the list of uninstallable applications.
        /// </summary>
        /// <param name="target">The application being added.</param>
        /// <param name="machineWide">Apply the registration machine-wide instead of just for the current user.</param>
        /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        public static void Register(FeedTarget target, IIconStore iconStore, bool machineWide)
        {
            string[] uninstallCommand = {Path.Combine(Locations.InstallBase, "0install-win.exe"), "remove", target.Uri.ToStringRfc()};
            if (machineWide) uninstallCommand = uninstallCommand.Append("--machine");

            Register(
                target.Uri.PrettyEscape(),
                uninstallCommand,
                target.Feed.Name + " (Zero Install)",
                target.Feed.Homepage,
                GetIconPath(target.Feed, iconStore),
                machineWide: machineWide);
        }

        private static string? GetIconPath(Feed feed, IIconStore iconStore)
        {
            var icon = feed.Icons.GetIcon(Icon.MimeTypeIco);
            return icon?.To(iconStore.Get);
        }

        /// <summary>
        /// Adds an entry to the list of uninstallable applications.
        /// </summary>
        /// <param name="id">The ID of the entry to create.</param>
        /// <param name="uninstallCommand">The command-line to invoke for uninstalling the application.</param>
        /// <param name="name">The name of the application.</param>
        /// <param name="homepage">The homepage of the application.</param>
        /// <param name="iconPath">The path of an icon file.</param>
        /// <param name="version">The application's current version.</param>
        /// <param name="size">The application's size in bytes.</param>
        /// <param name="machineWide">Apply the registration machine-wide instead of just for the current user.</param>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        public static void Register(string id, string[] uninstallCommand, string name, Uri? homepage = null, string? iconPath = null, string? version = null, long? size = null, bool machineWide = false)
        {
            using var uninstallKey = OpenUninstallKey(machineWide);
            using var appKey = uninstallKey.CreateSubKeyChecked(id);

            appKey.SetValue("UninstallString", uninstallCommand.JoinEscapeArguments());
            appKey.SetValue("QuietUninstallString", uninstallCommand.Concat(new[] {"--batch", "--background"}).JoinEscapeArguments());
            appKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
            appKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            appKey.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
            appKey.SetValue("DisplayName", name);
            appKey.SetOrDelete("DisplayIcon", iconPath);
            appKey.SetOrDelete("URLInfoAbout", homepage?.ToString());
            appKey.SetOrDelete("DisplayVersion", version);
            if (size.HasValue) appKey.SetValue("EstimatedSize", size / 1024, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Removes an entry from the list of uninstallable applications.
        /// </summary>
        /// <param name="uri">The feed to be removed.</param>
        /// <param name="machineWide">Apply the registration machine-wide instead of just for the current user.</param>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        public static void Unregister(FeedUri uri, bool machineWide)
            => Unregister(uri.PrettyEscape(), machineWide);

        /// <summary>
        /// Removes an entry from the list of uninstallable applications.
        /// </summary>
        /// <param name="id">The ID of the entry to be removed.</param>
        /// <param name="machineWide">Apply the registration machine-wide instead of just for the current user.</param>
        /// <exception cref="IOException">A problem occurred while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        public static void Unregister(string id, bool machineWide)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            #endregion

            using var key = OpenUninstallKey(machineWide);
            key.DeleteSubKey(id, throwOnMissingSubKey: false);
        }

        private static RegistryKey OpenUninstallKey(bool machineWide)
            => (machineWide ? Registry.LocalMachine : Registry.CurrentUser)
               .OpenSubKeyChecked(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", writable: true);
    }
}
