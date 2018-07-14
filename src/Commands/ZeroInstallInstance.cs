// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Info;
using NanoByte.Common.Native;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Services;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Provides information about the currently running instance of Zero Install.
    /// </summary>
    public static class ZeroInstallInstance
    {
        /// <summary>
        /// The version number of the currently running instance of Zero Install.
        /// </summary>
        public static ImplementationVersion Version => new ImplementationVersion(AppInfo.CurrentLibrary.Version);

        /// <summary>
        /// Indicates whether Zero Install is running from an implementation cache.
        /// </summary>
        public static bool IsRunningFromCache => StoreUtils.DetectImplementationPath(Locations.InstallBase) != null;

        /// <summary>
        /// Indicates whether Zero Install is running from a user-specific location.
        /// </summary>
        public static bool IsRunningFromPerUserDir => Locations.InstallBase.StartsWith(Locations.HomeDir);

        /// <summary>
        /// Tries to find another instance of Zero Install deployed on this system.
        /// </summary>
        /// <param name="needsMachineWide"><c>true</c> if a machine-wide install location is required; <c>false</c> if a user-specific location will also do.</param>
        /// <returns>The installation directory of another instance of Zero Install; <c>null</c> if none was found.</returns>
        [CanBeNull]
        public static string FindOther(bool needsMachineWide = true)
        {
            if (!WindowsUtils.IsWindows) return null;

            string installLocation = RegistryUtils.GetSoftwareString("Zero Install", "InstallLocation");
            if (string.IsNullOrEmpty(installLocation)) return null;
            if (installLocation == Locations.InstallBase) return null;
            if (needsMachineWide && installLocation.StartsWith(Locations.HomeDir)) return null;
            if (!File.Exists(Path.Combine(installLocation, "0install.exe"))) return null;
            return installLocation;
        }

        private static string DisableBackgroundUpdateFile => Path.Combine(Locations.PortableBase, "_no_self_update_check");

        /// <summary>
        /// Disables background update checks for Zero Install.
        /// </summary>
        public static void DisableBackgroundUpdate() => FileUtils.Touch(DisableBackgroundUpdateFile);

        /// <summary>
        /// Determines whether a background update check for Zero Install is currently allowed.
        /// </summary>
        public static bool IsBackgroundUpdateAllowed
            => !File.Exists(DisableBackgroundUpdateFile)
            && !IsRunningFromCache
            && NetUtils.IsInternetConnected;

        /// <summary>
        /// Checks if an update for Zero Install is available.
        /// </summary>
        /// <returns>The version number of the newest available update; <c>null</c> if no update is available.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">A problem occured while reading the feed file.</exception>
        /// <exception cref="WebException">A problem occured while fetching the feed file.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the cache is not permitted.</exception>
        /// <exception cref="SignatureException">The signature data of a remote feed file could not be verified.</exception>
        /// <exception cref="UriFormatException"><see cref="Config.SelfUpdateUri"/> is invalid.</exception>
        /// <exception cref="SolverException">The solver was unable to get information about the current version of Zero Install.</exception>
        /// <exception cref="InvalidDataException">A configuration file is damaged.</exception>
        [CanBeNull]
        public static ImplementationVersion UpdateCheck(ITaskHandler handler)
        {
            var services = new ServiceLocator(handler) {FeedManager = {Refresh = true}};
            if (services.Config.NetworkUse == NetworkLevel.Offline) return null;

            var requirements = new Requirements(services.Config.SelfUpdateUri);
            var selections = services.Solver.Solve(requirements);

            var newVersion = selections.MainImplementation.Version;
            return (newVersion > Version) ? newVersion : null;
        }
    }
}
