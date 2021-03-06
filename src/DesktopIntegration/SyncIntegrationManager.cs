// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common;
using NanoByte.Common.Dispatch;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Model;
using ZeroInstall.Model.Capabilities;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Synchronizes the <see cref="AppList"/> with other computers.
    /// </summary>
    /// <remarks>
    /// To prevent race-conditions there may only be one desktop integration class instance active at any given time.
    /// This class acquires a mutex upon calling its constructor and releases it upon calling <see cref="IDisposable.Dispose"/>.
    /// </remarks>
    public class SyncIntegrationManager : IntegrationManager
    {
        /// <summary>
        /// The suffix added to the <see cref="AppList"/> path to store a copy of the state at the last sync point.
        /// </summary>
        public const string AppListLastSyncSuffix = ".last-sync";

        /// <summary>Callback method used to retrieve additional <see cref="Feed"/>s on demand.</summary>
        private readonly Converter<FeedUri, Feed> _feedRetriever;

        /// <summary>The storage location of the <see cref="AppList"/> file.</summary>
        private readonly AppList _appListLastSync;

        /// <summary>
        /// Creates a new sync manager. Performs Mutex-based locking!
        /// </summary>
        /// <param name="config">Configuration for communicating with a sync server.</param>
        /// <param name="feedRetriever">Callback method used to retrieve additional <see cref="Feed"/>s on demand.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about the progress of long-running operations such as downloads.</param>
        /// <param name="machineWide">Apply operations machine-wide instead of just for the current user.</param>
        /// <exception cref="IOException">A problem occurred while accessing the <see cref="AppList"/> file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to the <see cref="AppList"/> file is not permitted or another desktop integration class is currently active.</exception>
        /// <exception cref="InvalidDataException">A problem occurred while deserializing the XML data.</exception>
        public SyncIntegrationManager(Config config, Converter<FeedUri, Feed> feedRetriever, ITaskHandler handler, bool machineWide = false)
            : base(config, handler, machineWide)
        {
            if (!Config.IsSyncConfigured)
                throw new InvalidDataException(Resources.PleaseConfigSync);

            _feedRetriever = feedRetriever ?? throw new ArgumentNullException(nameof(feedRetriever));

            if (File.Exists(AppListPath + AppListLastSyncSuffix)) _appListLastSync = XmlStorage.LoadXml<AppList>(AppListPath + AppListLastSyncSuffix);
            else
            {
                _appListLastSync = new AppList();
                _appListLastSync.SaveXml(AppListPath + AppListLastSyncSuffix);
            }
        }

        /// <summary>
        /// Synchronize the <see cref="AppList"/> with the sync server and (un)apply <see cref="AccessPoint"/>s accordingly.
        /// </summary>
        /// <param name="resetMode">Controls how synchronization data is reset.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="InvalidDataException">A problem occurred while deserializing the XML data or the specified crypto key was wrong.</exception>
        /// <exception cref="KeyNotFoundException">An <see cref="AccessPoint"/> reference to a <see cref="Capability"/> is invalid.</exception>
        /// <exception cref="ConflictException">One or more new <see cref="AccessPoint"/> would cause a conflict with the existing <see cref="AccessPoint"/>s in <see cref="AppList"/>.</exception>
        /// <exception cref="WebException">A problem occurred while communicating with the sync server or while downloading additional data (such as icons).</exception>
        /// <exception cref="IOException">A problem occurs while writing to the filesystem or registry.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
        public void Sync(SyncResetMode resetMode = SyncResetMode.None)
        {
            using var webClient = new WebClientTimeout
            {
                Credentials = new NetworkCredential(Config.SyncServerUsername, Config.SyncServerPassword),
                CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
            };
            var uri = new Uri(Config.SyncServer!, new Uri(MachineWide ? "app-list-machine" : "app-list", UriKind.Relative));

            ExceptionUtils.Retry<SyncRaceException>(delegate
            {
                Handler.CancellationToken.ThrowIfCancellationRequested();

                var data = DownloadAppList(webClient, uri, resetMode);
                HandleDownloadedAppList(data, resetMode);
                UploadAppList(webClient, uri, resetMode);
            }, maxRetries: 3);

            // Save reference point for future syncs
            AppList.SaveXml(AppListPath + AppListLastSyncSuffix);

            Handler.CancellationToken.ThrowIfCancellationRequested();
        }

        private byte[] DownloadAppList(WebClient webClient, Uri uri, SyncResetMode resetMode)
        {
            if (resetMode == SyncResetMode.Server) return Array.Empty<byte>();

            if (uri.IsFile)
            {
                try
                {
                    return File.ReadAllBytes(uri.LocalPath);
                }
                #region Error handling
                catch (FileNotFoundException)
                {
                    return Array.Empty<byte>();
                }
                #endregion
            }
            else
            {
                try
                {
                    byte[] data = null!;
                    Handler.RunTask(new SimpleTask(Resources.SyncDownloading, () => { data = webClient.DownloadData(uri); }));
                    return data;
                }
                #region Error handling
                catch (WebException ex) when (ex.Response is HttpWebResponse {StatusCode: HttpStatusCode.Unauthorized})
                {
                    Handler.CancellationToken.ThrowIfCancellationRequested();
                    throw new WebException(Resources.SyncCredentialsInvalid, ex, ex.Status, ex.Response);
                }
                #endregion
            }
        }

        /// <summary>
        /// Upload the encrypted AppList back to the server (unless the client was reset)
        /// </summary>
        private void UploadAppList(WebClient webClient, Uri uri, SyncResetMode resetMode)
        {
            if (resetMode == SyncResetMode.Client) return;

            var memoryStream = new MemoryStream();
            AppList.SaveXmlZip(memoryStream, Config.SyncCryptoKey);

            if (uri.IsFile)
            {
                memoryStream.CopyToFile(uri.LocalPath);
            }
            else
            {
                // Prevent "lost updates" (race conditions) with HTTP ETags
                if (resetMode == SyncResetMode.None && uri.Scheme is "http" or "https")
                {
                    if (!string.IsNullOrEmpty(webClient.ResponseHeaders?[HttpResponseHeader.ETag]))
                        webClient.Headers[HttpRequestHeader.IfMatch] = webClient.ResponseHeaders[HttpResponseHeader.ETag];
                }

                try
                {
                    Handler.RunTask(new SimpleTask(Resources.SyncUploading, () => webClient.UploadData(uri, "PUT", memoryStream.ToArray())));
                }
                catch (WebException ex) when (ex.Response is HttpWebResponse {StatusCode: HttpStatusCode.PreconditionFailed})
                {
                    Handler.CancellationToken.ThrowIfCancellationRequested();
                    throw new SyncRaceException(ex);
                }
                finally
                {
                    webClient.Headers.Remove(HttpRequestHeader.IfMatch);
                }
            }
        }

        private void HandleDownloadedAppList(byte[] appListData, SyncResetMode resetMode)
        {
            if (appListData.Length == 0) return;

            AppList serverList;
            try
            {
                serverList = AppList.LoadXmlZip(new MemoryStream(appListData), Config.SyncCryptoKey);
            }
            #region Error handling
            catch (ZipException ex)
            {
                // Wrap exception to add context information
                if (ex.Message == "Invalid password") throw new InvalidDataException(Resources.SyncCryptoKeyInvalid);
                throw new InvalidDataException(Resources.SyncServerDataDamaged, ex);
            }
            #endregion

            Handler.CancellationToken.ThrowIfCancellationRequested();
            try
            {
                MergeData(serverList, resetClient: resetMode == SyncResetMode.Client);
            }
            catch (KeyNotFoundException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new InvalidDataException(ex.Message, ex);
            }
            finally
            {
                Finish();
            }

            Handler.CancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Merges a new <see cref="IntegrationManagerBase.AppList"/> with the existing data. Performs a three-way merge using <see cref="_appListLastSync"/> as the base.
        /// </summary>
        /// <param name="remoteAppList">The remote <see cref="AppList"/> to merge in.</param>
        /// <param name="resetClient">Set to <c>true</c> to completely replace the contents of <see cref="IIntegrationManager.AppList"/> with <paramref name="remoteAppList"/> instead of merging the two.</param>
        private void MergeData(AppList remoteAppList, bool resetClient)
        {
            var toAdd = new List<AppEntry>();
            var toRemove = new List<AppEntry>();

            if (resetClient)
            {
                Merge.TwoWay(
                    theirs: remoteAppList.Entries,
                    mine: AppList.Entries,
                    added: toAdd, removed: toRemove);
            }
            else
            {
                Merge.ThreeWay(
                    reference: _appListLastSync.Entries,
                    theirs: remoteAppList.Entries,
                    mine: AppList.Entries,
                    added: toAdd, removed: toRemove);
            }

            void AddAppHelper(AppEntry prototype) => AddAppInternal(prototype, _feedRetriever);

            Handler.RunTask(new SimpleTask(Resources.ApplyingChanges, () =>
            {
                toRemove.ApplyWithRollback(RemoveAppInternal, AddAppHelper);
                toAdd.ApplyWithRollback(AddAppHelper, RemoveAppInternal);
            }));
        }
    }
}
