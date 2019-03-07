// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Native;
using ZeroInstall.Store;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Preferences;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Generates <see cref="SelectionCandidate"/>s for <see cref="ISolver"/>s to choose among.
    /// </summary>
    /// <remarks>Caches loaded <see cref="Feed"/>s, preferences, etc..</remarks>
    public class SelectionCandidateProvider : ISelectionCandidateProvider
    {
        private readonly Config _config;
        private readonly IPackageManager _packageManager;

        private readonly TransparentCache<FeedUri, InterfacePreferences> _interfacePreferences;
        private readonly Dictionary<string, ExternalImplementation> _externalImplementations;
        private readonly TransparentCache<FeedUri, Feed> _feeds;
        private readonly TransparentCache<ManifestDigest, bool> _storeContains;

        /// <summary>
        /// Creates a new <see cref="SelectionCandidate"/> provider.
        /// </summary>
        /// <param name="config">User settings controlling network behaviour, solving, etc.</param>
        /// <param name="feedManager">Provides access to remote and local <see cref="Feed"/>s. Handles downloading, signature verification and caching.</param>
        /// <param name="implementationStore">Used to check which <see cref="Implementation"/>s are already cached.</param>
        /// <param name="packageManager">An external package manager that can install <see cref="PackageImplementation"/>s.</param>
        public SelectionCandidateProvider([NotNull] Config config, [NotNull] IFeedManager feedManager, [NotNull] IImplementationStore implementationStore, [NotNull] IPackageManager packageManager)
        {
            #region Sanity checks
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (feedManager == null) throw new ArgumentNullException(nameof(feedManager));
            if (implementationStore == null) throw new ArgumentNullException(nameof(implementationStore));
            if (packageManager == null) throw new ArgumentNullException(nameof(packageManager));
            #endregion

            _config = config;
            _packageManager = packageManager;

            _interfacePreferences = new TransparentCache<FeedUri, InterfacePreferences>(InterfacePreferences.LoadForSafe);
            _externalImplementations = new Dictionary<string, ExternalImplementation>();
            _storeContains = new TransparentCache<ManifestDigest, bool>(implementationStore.Contains);
            _feeds = new TransparentCache<FeedUri, Feed>(feedUri =>
            {
                try
                {
                    var feed = feedManager[feedUri];
                    if (feed.MinInjectorVersion != null && FeedElement.ZeroInstallVersion < feed.MinInjectorVersion)
                    {
                        Log.Warn($"The Zero Install version is too old. The feed '{feedUri}' requires at least version {feed.MinInjectorVersion} but the installed version is {FeedElement.ZeroInstallVersion}. Try updating Zero Install.");
                        return null;
                    }
                    return feed;
                }
                catch (WebException ex)
                {
                    Log.Warn(ex);
                    return null;
                }
            });
        }

        /// <inheritdoc/>
        public IList<SelectionCandidate> GetSortedCandidates(Requirements requirements)
        {
            #region Sanity checks
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));
            #endregion

            var stabilityPolicy = _interfacePreferences[requirements.InterfaceUri].StabilityPolicy;
            if (stabilityPolicy == Stability.Unset) stabilityPolicy = _config.HelpWithTesting ? Stability.Testing : Stability.Stable;

            var candidates = GetFeeds(requirements).SelectMany(x => GetCandidates(x.Key, x.Value, requirements)).ToList();
            candidates.Sort(new SelectionCandidateComparer(stabilityPolicy, _config.NetworkUse, requirements.Languages, IsCached));
            return candidates;
        }

        /// <inheritdoc/>
        public Implementation LookupOriginalImplementation(ImplementationSelection implemenationSelection)
        {
            #region Sanity checks
            if (implemenationSelection == null) throw new ArgumentNullException(nameof(implemenationSelection));
            #endregion

            return (implemenationSelection.ID.StartsWith(ExternalImplementation.PackagePrefix)
                       ? _externalImplementations[implemenationSelection.ID]
                       : _feeds[implemenationSelection.FromFeed ?? implemenationSelection.InterfaceUri][implemenationSelection.ID])
                ?? throw new KeyNotFoundException();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _interfacePreferences.Clear();
            _externalImplementations.Clear();
            _feeds.Clear();
            _storeContains.Clear();
        }

        /// <summary>
        /// Loads the main feed for the specified <paramref name="requirements"/>, additional feeds added by local configuration and <see cref="Feed.Feeds"/> references.
        /// </summary>
        /// <returns>A dictionary mapping <see cref="FeedUri"/>s to the actual <see cref="Feed"/>s loaded from there.</returns>
        private IDictionary<FeedUri, Feed> GetFeeds(Requirements requirements)
        {
            var dictionary = new Dictionary<FeedUri, Feed>();

            void AddFeedToDict(FeedUri feedUri)
            {
                if (feedUri == null || dictionary.ContainsKey(feedUri)) return;
                var feed = _feeds[feedUri];
                if (feed == null) return;

                dictionary.Add(feedUri, feed);
                foreach (var reference in feed.Feeds.Where(x =>
                    x.Architecture.IsCompatible(requirements.Architecture) &&
                    (x.Languages.Count == 0 || x.Languages.ContainsAny(requirements.Languages, ignoreCountry: true))))
                    AddFeedToDict(reference.Source);
            }

            AddFeedToDict(requirements.InterfaceUri);
            foreach (string path in GetNativeFeedPaths(requirements.InterfaceUri))
                AddFeedToDict(new FeedUri(path));
            foreach (string path in GetSitePackagePaths(requirements.InterfaceUri))
                AddFeedToDict(new FeedUri(path));
            foreach (var reference in _interfacePreferences[requirements.InterfaceUri].Feeds)
            {
                try
                {
                    AddFeedToDict(reference.Source);
                }
                catch (IOException ex)
                {
                    throw new IOException(
                        string.Format("Failed to load feed {1} manually registered for interface {0}. Try '0install remove-feed {0} {1}'.", requirements.InterfaceUri.ToStringRfc(), reference.Source.ToStringRfc()),
                        ex);
                }
            }

            return dictionary;
        }

        private static IEnumerable<string> GetNativeFeedPaths(FeedUri interfaceUri)
            => Locations.GetLoadDataPaths("0install.net", true, "native_feeds", interfaceUri.PrettyEscape());

        private static IEnumerable<string> GetSitePackagePaths(FeedUri interfaceUri)
        {
            var sitePackageDirs = Locations.GetLoadDataPaths("0install.net", isFile: false, resource: interfaceUri.EscapeComponent().Prepend("site-packages"));
            var subDirectories = sitePackageDirs.SelectMany(x => new DirectoryInfo(x).GetDirectories());
            return subDirectories.Select(dir => Path.Combine(dir.FullName, "0install" + Path.DirectorySeparatorChar + "feed.xml")).Where(File.Exists);
        }

        private IEnumerable<SelectionCandidate> GetCandidates(FeedUri feedUri, Feed feed, Requirements requirements)
        {
            var feedPreferences = FeedPreferences.LoadForSafe(feedUri);

            foreach (var element in feed.Elements)
            {
                if (element is PackageImplementation packageImplementation)
                { // Each <package-implementation> provides 0..n selection candidates
                    var externalImplementations = _packageManager.Query(packageImplementation, requirements.Distributions.ToArray());
                    foreach (var externalImplementation in externalImplementations)
                    {
                        _externalImplementations[externalImplementation.ID] = externalImplementation;
                        yield return new SelectionCandidate(new FeedUri(FeedUri.FromDistributionPrefix + feedUri), feedPreferences, externalImplementation, requirements);
                    }
                }
                else if (requirements.Distributions.ContainsOrEmpty(Restriction.DistributionZeroInstall))
                {
                    if (element is Implementation implementation)
                    { // Each <implementation> provides 1 selection candidate
                        yield return new SelectionCandidate(feedUri, feedPreferences, implementation, requirements,
                            offlineUncached: (_config.NetworkUse == NetworkLevel.Offline) && !IsCached(implementation));
                    }
                }
            }
        }

        private bool IsCached(Implementation implementation)
        {
            if (!string.IsNullOrEmpty(implementation.LocalPath)) return true;
            if (implementation is ExternalImplementation externalImplementation) return externalImplementation.IsInstalled;
            return _storeContains[implementation.ManifestDigest];
        }
    }
}
