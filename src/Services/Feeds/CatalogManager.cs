// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using NanoByte.Common.Net;
using NanoByte.Common.Streams;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Services.Feeds;

/// <summary>
/// Provides access to remote and local <see cref="Catalog"/>s. Handles downloading, signature verification and caching.
/// </summary>
[PrimaryConstructor]
public partial class CatalogManager : ICatalogManager
{
    #region Constants
    /// <summary>
    /// The default <see cref="Catalog"/> source used if no other is specified.
    /// </summary>
    public static readonly FeedUri DefaultSource = new("https://apps.0install.net/catalog.xml");

    private static readonly FeedUri _oldDefaultSource = new("http://0install.de/catalog/");
    #endregion

    private readonly Config _config;
    private readonly ITrustManager _trustManager;
    private readonly ITaskHandler _handler;

    private readonly string _cacheFilePath = Path.Combine(Locations.GetCacheDirPath("0install.net", machineWide: false), "catalog.xml");

    /// <inheritdoc/>
    [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "File system access")]
    public Catalog? GetCached()
    {
        try
        {
            return XmlStorage.LoadXml<Catalog>(_cacheFilePath);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Saves a merged <see cref="Catalog"/> to the cache file for later retrieval by <see cref="GetCached"/>.
    /// </summary>
    private void SaveCache(Catalog catalog)
    {
        try
        {
            Log.Debug("Caching Catalog in: " + _cacheFilePath);
            catalog.SaveXml(_cacheFilePath);
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn(Resources.UnableToCacheCatalog, ex);
        }
        #endregion
    }

    /// <inheritdoc/>
    [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Performs network IO and has side-effects")]
    public Catalog GetOnline()
    {
        try
        {
            var mergedCatalog = new Catalog
            {
                Feeds =
                {
                    GetSources().AsParallel()
                                .WithDegreeOfParallelism(_config.MaxParallelDownloads)
                                .WithCancellation(_handler.CancellationToken)
                                .Select(DownloadCatalog)
                                .SelectMany(x => x.Feeds)
                }
            };
            SaveCache(mergedCatalog);
            return mergedCatalog;
        }
        catch (AggregateException ex)
        {
            throw ex.RethrowFirstInner();
        }
    }

    /// <inheritdoc/>
    public Catalog DownloadCatalog(FeedUri source)
    {
        #region Sanity checks
        if (source == null) throw new ArgumentNullException(nameof(source));
        #endregion

        Catalog catalog = default!;
        if (source.IsFile)
            catalog = XmlStorage.LoadXml<Catalog>(source.LocalPath);
        else
        {
            if (_config.NetworkUse == NetworkLevel.Offline)
                throw new WebException(string.Format(Resources.NoDownloadInOfflineMode, source));

            _handler.RunTask(new DownloadFile(source, stream =>
            {
                byte[] data = stream.AsArray();
                _trustManager.CheckTrust(data, source);
                try
                {
                    catalog = XmlStorage.LoadXml<Catalog>(data.ToStream());
                }
                #region Error handling
                catch (InvalidDataException ex)
                {
                    // Change exception message to add context information
                    throw new InvalidDataException(string.Format(Resources.UnableToParseFeed, source), ex.InnerException);
                }
                #endregion
            }));
        }

        catalog.Normalize(source);
        return catalog;
    }

    /// <inheritdoc/>
    public bool AddSource(FeedUri uri)
    {
        var sources = GetSources().ToList();
        if (!sources.AddIfNew(uri)) return false;
        SetSources(sources);
        return true;
    }

    /// <inheritdoc/>
    public bool RemoveSource(FeedUri uri)
    {
        var sources = GetSources().ToList();
        if (!sources.Remove(uri)) return false;
        SetSources(sources);

        // Remove relevant entries from cache
        if (GetCached() is {} cached)
        {
            cached.Feeds.RemoveAll(x => x.CatalogUri == uri);
            SaveCache(cached);
        }

        return true;
    }

    /// <summary>
    /// Returns a list of catalog sources as defined by configuration files.
    /// </summary>
    /// <remarks>Only the top-most configuration file is processed. I.e., a user config overrides a system config.</remarks>
    /// <exception cref="IOException">There was a problem accessing a configuration file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file was not permitted.</exception>
    /// <exception cref="UriFormatException">An invalid catalog source is specified in the configuration file.</exception>
    [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Reads data from a config file with no caching")]
    public static FeedUri[] GetSources()
    {
        string? path = Locations.GetLoadConfigPaths("0install.net", true, "catalog-sources").FirstOrDefault();
        if (string.IsNullOrEmpty(path)) return new[] {DefaultSource};

        string[] ReadAllLines()
        {
            using (new AtomicRead(path))
                return File.ReadAllLines(path, Encoding.UTF8);
        }

        return ReadAllLines().Except(string.IsNullOrEmpty)
                             .Except(line => line.StartsWith("#"))
                             .Select(line => new FeedUri(line))
                             .Select(uri => uri == _oldDefaultSource ? DefaultSource : uri)
                             .ToArray();
    }

    /// <summary>
    /// Sets the list of catalog sources in a configuration file.
    /// </summary>
    /// <param name="uris">The list of catalog sources to use from now on.</param>
    /// <exception cref="IOException">There was a problem writing a configuration file.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file was not permitted.</exception>
    public static void SetSources(IEnumerable<FeedUri> uris)
    {
        #region Sanity checks
        if (uris == null) throw new ArgumentNullException(nameof(uris));
        #endregion

        using var atomic = new AtomicWrite(Locations.GetSaveConfigPath("0install.net", true, "catalog-sources"));
        using (var configFile = new StreamWriter(atomic.WritePath, append: false, EncodingUtils.Utf8) {NewLine = "\n"})
        {
            foreach (var uri in uris)
                configFile.WriteLine(uri.ToStringRfc());
        }
        atomic.Commit();
    }
}
