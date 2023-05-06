// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common.Dispatch;
using NanoByte.Common.Net;
using NanoByte.Common.Streams;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.DesktopIntegration;

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

    private readonly Uri _syncServer;
    private readonly Converter<FeedUri, Feed> _feedRetriever;
    private readonly AppList _appListLastSync;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates a new sync manager. Performs Mutex-based locking!
    /// </summary>
    /// <param name="config">Configuration for communicating with a sync server.</param>
    /// <param name="feedRetriever">Callback method used to retrieve additional <see cref="Feed"/>s on demand.</param>
    /// <param name="handler">A callback object used when the the user is to be informed about the progress of long-running operations such as downloads.</param>
    /// <param name="machineWide">Apply operations machine-wide instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while accessing the <see cref="AppList"/> file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read or write access to the <see cref="AppList"/> file is not permitted or another desktop integration class is currently active.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing an XML file.</exception>
    public SyncIntegrationManager(Config config, Converter<FeedUri, Feed> feedRetriever, ITaskHandler handler, bool machineWide = false)
        : base(config, handler, machineWide)
    {
        if (!config.IsSyncConfigured) throw new InvalidDataException(Resources.PleaseConfigSync);
        _syncServer = config.SyncServer.EnsureTrailingSlash();

        _feedRetriever = feedRetriever ?? throw new ArgumentNullException(nameof(feedRetriever));

        if (File.Exists(AppListPath + AppListLastSyncSuffix)) _appListLastSync = XmlStorage.LoadXml<AppList>(AppListPath + AppListLastSyncSuffix);
        else
        {
            _appListLastSync = new AppList();
            _appListLastSync.SaveXml(AppListPath + AppListLastSyncSuffix);
        }

        _httpClient = new()
        {
            DefaultRequestHeaders =
            {
                Authorization = new NetworkCredential(Config.SyncServerUsername, Config.SyncServerPassword).ToBasicAuth(),
                CacheControl = new() {NoCache = true}
            }
        };
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        try
        {
            _httpClient.Dispose();
        }
        finally
        {
            base.Dispose();
        }
    }

    /// <summary>
    /// Synchronize the <see cref="AppList"/> with the sync server and (un)apply <see cref="AccessPoint"/>s accordingly.
    /// </summary>
    /// <param name="resetMode">Controls how synchronization data is reset.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="InvalidDataException">A problem occurred while deserializing an XML file or the specified crypto key was wrong.</exception>
    /// <exception cref="KeyNotFoundException">An <see cref="AccessPoint"/> reference to a <see cref="Capability"/> is invalid.</exception>
    /// <exception cref="ConflictException">One or more new <see cref="AccessPoint"/> would cause a conflict with the existing <see cref="AccessPoint"/>s in <see cref="AppList"/>.</exception>
    /// <exception cref="WebException">A problem occurred while communicating with the sync server or while downloading additional data (such as icons).</exception>
    /// <exception cref="IOException">A problem occurs while writing to the filesystem or registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem or registry is not permitted.</exception>
    public void Sync(SyncResetMode resetMode = SyncResetMode.None)
    {
        var uri = new Uri(_syncServer, new Uri(MachineWide ? "app-list-machine" : "app-list", UriKind.Relative));

        ExceptionUtils.Retry<SyncRaceException>(() =>
        {
            Handler.CancellationToken.ThrowIfCancellationRequested();

            var (data, etag) = resetMode switch
            {
                SyncResetMode.Server => default,
                _ => DownloadAppList(uri)
            };

            HandleDownloadedAppList(data ?? Array.Empty<byte>(), resetClient: resetMode == SyncResetMode.Client);

            if (resetMode != SyncResetMode.Client)
                UploadAppList(uri, etag);
        }, maxRetries: 3);

        // Save reference point for future syncs
        AppList.SaveXml(AppListPath + AppListLastSyncSuffix);

        Handler.CancellationToken.ThrowIfCancellationRequested();
    }

    private (byte[]?, string? etag) DownloadAppList(Uri uri)
    {
        if (uri.IsFile)
        {
            try
            {
                return (File.ReadAllBytes(uri.LocalPath), null);
            }
            catch (FileNotFoundException)
            {
                return default;
            }
        }

        try
        {
            byte[]? data = null;
            string? etag = null;
            Handler.RunTask(new SimpleTask(Resources.SyncDownloading, () =>
            {
                using var response = _httpClient.Send(new(HttpMethod.Get, uri), Handler.CancellationToken);
                if (response.StatusCode == HttpStatusCode.Unauthorized) throw new WebException(Resources.SyncCredentialsInvalid);
                response.EnsureSuccessStatusCode();

                using var stream = response.Content.ReadAsStream(Handler.CancellationToken);
                data = stream.ReadAll().AsArray();
                etag = response.Headers.ETag?.Tag;
            }));
            return (data, etag);
        }
        #region Error handling
        catch (HttpRequestException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw ex.AsWebException();
        }
        #endregion
    }

    /// <summary>
    /// Upload the encrypted AppList back to the server (unless the client was reset)
    /// </summary>
    private void UploadAppList(Uri uri, string? lastEtag)
    {
        var memoryStream = new MemoryStream();
        AppList.SaveXmlZip(memoryStream, Config.SyncCryptoKey);

        if (uri.IsFile)
        {
            memoryStream.CopyToFile(uri.LocalPath);
            return;
        }

        Handler.RunTask(new SimpleTask(Resources.SyncUploading, () =>
        {
            try
            {
                memoryStream.Position = 0;
                var request = new HttpRequestMessage(HttpMethod.Put, uri) {Content = new StreamContent(memoryStream)};
                if (!string.IsNullOrEmpty(lastEtag)) request.Headers.IfMatch.Add(new(lastEtag));

                using var response = _httpClient.Send(request, Handler.CancellationToken);
                if (response.StatusCode == HttpStatusCode.PreconditionFailed) throw new SyncRaceException();
                response.EnsureSuccessStatusCode();
            }
            #region Error handling
            catch (HttpRequestException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw ex.AsWebException();
            }
            #endregion
        }));
    }

    private void HandleDownloadedAppList(byte[] appListData, bool resetClient)
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
            MergeData(serverList, resetClient);
        }
        #region Error handling
        catch (KeyNotFoundException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new InvalidDataException(ex.Message, ex);
        }
        #endregion
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

        Handler.RunTask(ForEachTask.Create(Resources.ApplyingChanges, toRemove, RemoveAppInternal, AddAppHelper));
        Handler.RunTask(ForEachTask.Create(Resources.ApplyingChanges, toAdd, AddAppHelper, RemoveAppInternal));
    }
}
