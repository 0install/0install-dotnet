// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Services;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Provides information about the currently running instance of Zero Install.
    /// </summary>
    public static class ZeroInstallInstance
    {
        /// <summary>
        /// Indicates whether Zero Install is running from an implementation cache.
        /// </summary>
        public static bool IsRunningFromCache
            => ImplementationStoreUtils.DetectImplementationPath(Locations.InstallBase) != null;

        /// <summary>
        /// Indicates whether Zero Install is running from a user-specific location.
        /// </summary>
        public static bool IsRunningFromPerUserDir
            => Locations.InstallBase.StartsWith(Locations.HomeDir);

        /// <summary>
        /// Tries to find another instance of Zero Install deployed on this system.
        /// </summary>
        /// <param name="needsMachineWide"><c>true</c> if a machine-wide install location is required; <c>false</c> if a user-specific location will also do.</param>
        /// <returns>The installation directory of another instance of Zero Install; <c>null</c> if none was found.</returns>
        public static string? FindOther(bool needsMachineWide = true)
        {
            if (!WindowsUtils.IsWindows) return null;

            string? installLocation = RegistryUtils.GetSoftwareString("Zero Install", "InstallLocation");
            if (string.IsNullOrEmpty(installLocation)) return null;
            if (installLocation == Locations.InstallBase) return null;
            if (needsMachineWide && installLocation.StartsWith(Locations.HomeDir)) return null;
            if (!File.Exists(Path.Combine(installLocation, "0install.exe"))) return null;
            return installLocation;
        }

        /// <summary>
        /// Silently checks if an update for Zero Install is available.
        /// </summary>
        /// <returns>The version number of the newest available update; <c>null</c> if no update is available.</returns>
        public static ImplementationVersion? SilentUpdateCheck()
        {
            if (IsRunningFromCache || !NetUtils.IsInternetConnected) return null;

            using var handler = new SilentTaskHandler();
            var services = new ServiceLocator(handler) {FeedManager = {Refresh = true}};
            if (services.Config.NetworkUse == NetworkLevel.Offline || services.Config.SelfUpdateUri == null) return null;

            var requirements = new Requirements(services.Config.SelfUpdateUri);

            try
            {
                var selections = services.Solver.Solve(requirements);
                var newVersion = selections.MainImplementation.Version;
                return (newVersion > ImplementationVersion.ZeroInstall) ? newVersion : null;
            }
            #region Error handling
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (WebException ex)
            {
                Log.Debug(ex);
                return null;
            }
            catch (IOException ex)
            {
                Log.Warn("Problem with silent self-update check");
                Log.Warn(ex);
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warn("Problem with silent self-update check");
                Log.Warn(ex);
                return null;
            }
            catch (SignatureException ex)
            {
                Log.Warn("Problem with silent self-update check");
                Log.Warn(ex);
                return null;
            }
            catch (UriFormatException ex)
            {
                Log.Warn("Problem with silent self-update check");
                Log.Warn(ex);
                return null;
            }
            catch (SolverException ex)
            {
                Log.Warn("Problem with silent self-update check");
                Log.Warn(ex);
                return null;
            }
            catch (InvalidDataException ex)
            {
                Log.Warn("Problem with silent self-update check");
                Log.Warn(ex);
                return null;
            }
            #endregion
        }
    }
}
