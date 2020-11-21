// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Services.Properties;
using ZeroInstall.Store.Feeds;

namespace ZeroInstall.Services.Feeds
{
    /// <summary>
    /// Provides access to remote and local <see cref="Catalog"/>s. Handles downloading, signature verification and caching.
    /// </summary>
    public class CatalogManager : ICatalogManager
    {
        #region Constants
        private const string CacheMutexName = "ZeroInstall.Feeds.CatalogManager.Cache";

        /// <summary>
        /// The default <see cref="Catalog"/> source used if no other is specified.
        /// </summary>
        public static readonly FeedUri DefaultSource = new FeedUri("https://apps.0install.net/catalog.xml");

        private static readonly FeedUri _oldDefaultSource = new FeedUri("http://0install.de/catalog/");
        #endregion

        #region Dependencies
        private readonly ITrustManager _trustManager;
        private readonly ITaskHandler _handler;

        /// <summary>
        /// Creates a new catalog manager.
        /// </summary>
        /// <param name="trustManager">Methods for verifying signatures and user trust.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about progress.</param>
        public CatalogManager(ITrustManager trustManager, ITaskHandler handler)
        {
            _trustManager = trustManager ?? throw new ArgumentNullException(nameof(trustManager));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }
        #endregion

        private readonly string _cacheFilePath = Path.Combine(Locations.GetCacheDirPath("0install.net", machineWide: false), "catalog.xml");

        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "File system access")]
        public Catalog? GetCached()
        {
            try
            {
                using (new MutexLock(CacheMutexName))
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
                using (new MutexLock(CacheMutexName))
                    catalog.SaveXml(_cacheFilePath);
            }
            #region Error handling
            catch (IOException ex)
            {
                Log.Warn(Resources.UnableToCacheCatalog);
                Log.Warn(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warn(Resources.UnableToCacheCatalog);
                Log.Warn(ex.Message);
            }
            #endregion
        }

        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Performs network IO and has side-effects")]
        public Catalog GetOnline()
        {
            // Download + merge
            var mergedCatalog = new Catalog();
            foreach (var source in GetSources())
            {
                foreach (var feed in DownloadCatalog(source).Feeds)
                {
                    feed.CatalogUri = source;
                    mergedCatalog.Feeds.Add(feed);
                }
            }
            mergedCatalog.Normalize();

            SaveCache(mergedCatalog);

            return mergedCatalog;
        }

        /// <inheritdoc/>
        public Catalog DownloadCatalog(FeedUri source)
        {
            #region Sanity checks
            if (source == null) throw new ArgumentNullException(nameof(source));
            #endregion

            if (source.IsFile) return XmlStorage.LoadXml<Catalog>(source.LocalPath);

            var download = new DownloadMemory(source);
            _handler.RunTask(download);
            var data = download.GetData();
            _trustManager.CheckTrust(data, source);
            return XmlStorage.LoadXml<Catalog>(new MemoryStream(data));
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
            var cached = GetCached();
            if (cached != null)
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
            using (var configFile = new StreamWriter(atomic.WritePath, append: false, encoding: FeedUtils.Encoding) {NewLine = "\n"})
            {
                foreach (var uri in uris)
                    configFile.WriteLine(uri.ToStringRfc());
            }
            atomic.Commit();
        }
    }
}
