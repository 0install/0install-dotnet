// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Commands.Desktop;
using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Model;
using ZeroInstall.Services;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Common base class for Zero Install operations that require scoped dependency resolution.
    /// </summary>
    public abstract class ScopedOperation : ServiceLocator
    {
        /// <summary>
        /// Creates a new command base.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        protected ScopedOperation(ITaskHandler handler)
            : base(handler)
        {}

        /// <summary>
        /// Converts an interface or feed URI to its canonical representation.
        /// </summary>
        /// <exception cref="UriFormatException"><paramref name="uri"/> is an invalid interface URI.</exception>
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#", Justification = "This method handles a number of non-standard URI types which cannot be represented by the regular Uri class.")]
        public FeedUri GetCanonicalUri(string uri)
        {
            if (string.IsNullOrEmpty(uri)) throw new UriFormatException();

            try
            {
                if (uri.StartsWith("file://")) return new(uri);
                if (uri.StartsWith("file:/")) throw new UriFormatException(Resources.FilePrefixAbsoluteUsage);
                if (uri.StartsWith("file:", out string? path)) return new(Path.GetFullPath(path));
                if (uri.StartsWith("http:") || uri.StartsWith("https:")) return new(uri);

                var result = TryResolveAlias(uri);
                if (result != null) return result;

                if (Path.IsPathRooted(uri)) return new(uri);

                path = Path.GetFullPath(WindowsUtils.IsWindows ? Environment.ExpandEnvironmentVariables(uri) : uri);
                if (File.Exists(path)) return new(path);

                result = TryResolveCatalog(uri);
                if (result != null) return result;

                return new(path);
            }
            #region Error handling
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
            {
                // Wrap exception since only certain exception types are allowed
                throw new UriFormatException(ex.Message);
            }
            #endregion
        }

        private static FeedUri? TryResolveAlias(string uri)
        {
            var appList = AppList.LoadSafe();

            const string aliasPrefix = "alias:";
            if (uri.StartsWith(aliasPrefix, out string? aliasName))
            {
                var result = appList.ResolveAlias(aliasName);

                if (result == null)
                    throw new UriFormatException(string.Format(Resources.AliasNotFound, aliasName));
                return result;
            }
            else
            {
                aliasName = uri;
                var result = appList.ResolveAlias(aliasName);

                if (result != null)
                    Log.Info(string.Format(Resources.ResolvedUsingAlias, aliasName, result));
                return result;
            }
        }

        private FeedUri? TryResolveCatalog(string shortName)
        {
            var feed = FindByShortName(shortName);
            if (feed == null) return null;

            Log.Info(string.Format(Resources.ResolvedUsingCatalog, shortName, feed.Uri));
            return feed.Uri;
        }

        /// <summary>
        /// Returns a merged view of all <see cref="Catalog"/>s specified by the configuration files.
        /// </summary>
        /// <remarks>Handles caching based on <see cref="FeedManager.Refresh"/> flag.</remarks>
        /// <exception cref="WebException">Attempted to download catalog and failed.</exception>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Performs network IO")]
        protected Catalog GetCatalog()
        {
            Catalog? result = null;
            if (!FeedManager.Refresh) result = CatalogManager.GetCached();
            if (result == null && Config.NetworkUse != NetworkLevel.Offline) result = CatalogManager.GetOnlineSafe();
            if (result == null) throw new WebException(Resources.UnableToLoadCatalog);
            return result;
        }

        /// <summary>
        /// Uses <see cref="Catalog.FindByShortName"/> to find a <see cref="Feed"/> matching a specific short name.
        /// </summary>
        /// <param name="shortName">The short name to look for. Must match either <see cref="Feed.Name"/> or <see cref="EntryPoint.BinaryName"/> of <see cref="Command.NameRun"/>.</param>
        /// <returns>The first matching <see cref="Feed"/>; <c>null</c> if no match was found.</returns>
        /// <remarks>Handles caching based on <see cref="FeedManager.Refresh"/> flag.</remarks>
        protected Feed? FindByShortName(string shortName)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(shortName)) throw new ArgumentNullException(nameof(shortName));
            #endregion

            Feed? result = null;
            if (!FeedManager.Refresh) result = CatalogManager.GetCachedSafe().FindByShortName(shortName);
            if (result == null && Config.NetworkUse != NetworkLevel.Offline) result = CatalogManager.GetOnlineSafe().FindByShortName(shortName);
            return result;
        }

        /// <summary>
        /// Periodically checks Zero Install itself for updates in a background process.
        /// </summary>
        protected void SelfUpdateCheck()
        {
            if (!ZeroInstallInstance.IsRunningFromCache
             && NetUtils.IsInternetConnected
             && Handler.Verbosity != Verbosity.Batch
             && Config.NetworkUse == NetworkLevel.Full
             && Config.SelfUpdateUri != null
             && FeedManager.IsStale(Config.SelfUpdateUri))
            {
                // Prevent multiple concurrent updates
                if (FeedManager.RateLimit(Config.SelfUpdateUri)) return;

                Log.Info("Starting periodic background self-update check");
                StartCommandBackground(Self.Name, Self.Update.Name);
            }
        }

        /// <summary>
        /// Starts executing a "0install" command in a background process. Returns immediately.
        /// </summary>
        /// <param name="command">The name of the command to execute.</param>
        /// <param name="args">Additional arguments to pass to the command.</param>
        protected static void StartCommandBackground(string command, params string[] args)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(command)) throw new ArgumentNullException(nameof(command));
            #endregion

            if (ProgramUtils.GuiAssemblyName == null)
            {
                Log.Info("Skipping background command because there is no GUI subsystem available");
                return;
            }

            try
            {
                var startInfo = ProcessUtils.Assembly(ProgramUtils.GuiAssemblyName, new[] {command, "--background"}.Concat(args).ToArray());
                startInfo.WorkingDirectory = Locations.InstallBase; // Avoid locking the user's working directory
                startInfo.Start();
            }
            #region Error handling
            catch (OperationCanceledException)
            {}
            catch (IOException ex)
            {
                Log.Warn(ex);
            }
            #endregion
        }
    }
}
