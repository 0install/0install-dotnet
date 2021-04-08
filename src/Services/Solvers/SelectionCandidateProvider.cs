// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using ZeroInstall.Model;
using ZeroInstall.Model.Preferences;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Native;
using ZeroInstall.Store;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Generates <see cref="SelectionCandidate"/>s for <see cref="ISolver"/>s to choose among.
    /// </summary>
    /// <remarks>Caches loaded <see cref="Feed"/>s, preferences, etc..</remarks>
    public class SelectionCandidateProvider : ISelectionCandidateProvider
    {
        private readonly Config _config;
        private readonly IFeedManager _feedManager;
        private readonly IPackageManager _packageManager;

        /// <summary>Indicates whether a specific implementation is already cached in an <see cref="IImplementationStore"/>.</summary>
        private readonly TransparentCache<ManifestDigest, bool> _storeContains;

        /// <summary>
        /// Creates a new <see cref="SelectionCandidate"/> provider.
        /// </summary>
        /// <param name="config">User settings controlling network behaviour, solving, etc.</param>
        /// <param name="feedManager">Provides access to remote and local <see cref="Feed"/>s. Handles downloading, signature verification and caching.</param>
        /// <param name="implementationStore">Used to check which <see cref="Implementation"/>s are already cached.</param>
        /// <param name="packageManager">An external package manager that can install <see cref="PackageImplementation"/>s.</param>
        public SelectionCandidateProvider(Config config, IFeedManager feedManager, IImplementationStore implementationStore, IPackageManager packageManager)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _feedManager = feedManager ?? throw new ArgumentNullException(nameof(feedManager));
            _packageManager = packageManager ?? throw new ArgumentNullException(nameof(packageManager));

            if (implementationStore == null) throw new ArgumentNullException(nameof(implementationStore));
            _storeContains = new(implementationStore.Contains);
        }

        /// <summary>Caches <see cref="InterfacePreferences"/>.</summary>
        private readonly TransparentCache<FeedUri, InterfacePreferences> _interfacePreferences = new(InterfacePreferences.LoadForSafe);

        /// <inheritdoc/>
        public IReadOnlyList<SelectionCandidate> GetSortedCandidates(Requirements requirements)
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

        /// <summary>Maps <see cref="ImplementationBase.ID"/>s to <see cref="ExternalImplementation"/>s.</summary>
        private readonly Dictionary<string, ExternalImplementation> _externalImplementations = new();

        /// <inheritdoc/>
        public Implementation LookupOriginalImplementation(ImplementationSelection implementationSelection)
        {
            #region Sanity checks
            if (implementationSelection == null) throw new ArgumentNullException(nameof(implementationSelection));
            #endregion

            return (implementationSelection.ID.StartsWith(ExternalImplementation.PackagePrefix)
                       ? _externalImplementations[implementationSelection.ID]
                       : _feedManager[implementationSelection.FromFeed ?? implementationSelection.InterfaceUri][implementationSelection.ID])
                ?? throw new KeyNotFoundException();
        }

        /// <summary>Records feeds that failed to download to prevent multiple attempts.</summary>
        private readonly HashSet<FeedUri> _failedFeeds = new();

        /// <summary>
        /// Loads the main feed for the specified <paramref name="requirements"/>, additional feeds added by local configuration and <see cref="Feed.Feeds"/> references.
        /// </summary>
        /// <returns>A dictionary mapping <see cref="FeedUri"/>s to the actual <see cref="Feed"/>s loaded from there.</returns>
        private IDictionary<FeedUri, Feed> GetFeeds(Requirements requirements)
        {
            var dictionary = new Dictionary<FeedUri, Feed>();

            void AddFeedToDict(FeedUri feedUri)
            {
                try
                {
                    if (dictionary.ContainsKey(feedUri) || _failedFeeds.Contains(feedUri)) return;
                    var feed = _feedManager[feedUri];
                    if (feed.MinInjectorVersion != null && ModelUtils.Version < feed.MinInjectorVersion)
                    {
                        _failedFeeds.Add(feedUri);
                        Log.Warn($"The Zero Install version is too old. The feed '{feedUri}' requires at least version {feed.MinInjectorVersion} but the installed version is {ModelUtils.Version}. Try updating Zero Install.");
                        return;
                    }

                    dictionary.Add(feedUri, feed);
                    foreach (var reference in feed.Feeds.Where(x =>
                        x.Architecture.RunsOn(requirements.Architecture) &&
                        (x.Languages.Count == 0 || x.Languages.ContainsAny(requirements.Languages, ignoreCountry: true))))
                        AddFeedToDict(reference.Source);
                }
                catch (WebException ex)
                {
                    _failedFeeds.Add(feedUri);
                    Log.Warn(ex);
                }
            }

            AddFeedToDict(requirements.InterfaceUri);
            foreach (string path in GetNativeFeedPaths(requirements.InterfaceUri))
                AddFeedToDict(new(path));
            foreach (string path in GetSitePackagePaths(requirements.InterfaceUri))
                AddFeedToDict(new(path));
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
            var feedPreferences = _feedManager.GetPreferences(feedUri);

            foreach (var element in feed.Elements)
            {
                if (element is PackageImplementation packageImplementation)
                { // Each <package-implementation> provides 0..n selection candidates
                    var externalImplementations = _packageManager.Query(packageImplementation, requirements.Distributions.ToArray());
                    foreach (var externalImplementation in externalImplementations)
                    {
                        _externalImplementations[externalImplementation.ID] = externalImplementation;
                        yield return new SelectionCandidate(new(FeedUri.FromDistributionPrefix + feedUri), feedPreferences, externalImplementation, requirements);
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
