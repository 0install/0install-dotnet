// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Concurrent;
using ZeroInstall.Model.Preferences;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Native;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Generates <see cref="SelectionCandidate"/>s for <see cref="ISolver"/>s to choose among.
/// </summary>
/// <remarks>This class performs in-memory caching of <see cref="InterfacePreferences"/>s and implementations and is thread-safe.</remarks>
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
    private readonly ConcurrentDictionary<string, ExternalImplementation> _externalImplementations = [];

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
    private readonly ConcurrentDictionary<FeedUri, Exception> _failedFeeds = [];

    /// <inheritdoc/>
    public IReadOnlyDictionary<FeedUri, Exception> FailedFeeds => _failedFeeds;

    /// <summary>Provides separate locks for each feed URI.</summary>
    private readonly TransparentCache<FeedUri, object> _feedLocks = new(_ => new());

    /// <summary>
    /// Loads the main feed for the specified <paramref name="requirements"/>, additional feeds added by local configuration and <see cref="Feed.Feeds"/> references.
    /// </summary>
    /// <returns>A dictionary mapping <see cref="FeedUri"/>s to the actual <see cref="Feed"/>s loaded from there.</returns>
    private IDictionary<FeedUri, Feed> GetFeeds(Requirements requirements)
    {
        var dictionary = new Dictionary<FeedUri, Feed>();

        void AddFeedToDict(FeedUri feedUri)
        {
            lock (_feedLocks[feedUri])
            {
                try
                {
                    if (dictionary.ContainsKey(feedUri) || _failedFeeds.ContainsKey(feedUri)) return;
                    var feed = _feedManager[feedUri];

                    dictionary.Add(feedUri, feed);
                    foreach (var reference in feed.Feeds.Where(x =>
                                 x.Architecture.RunsOn(requirements.Architecture) &&
                                 (x.Languages.Count == 0 || x.Languages.ContainsAny(requirements.Languages, ignoreCountry: true))))
                        AddFeedToDict(reference.Source);
                }
                catch (Exception ex) when (ex is NotSupportedException or WebException or InvalidDataException or SignatureException)
                {
                    Log.Info(ex);
                    _failedFeeds.TryAdd(feedUri, ex);
                }
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
        => Locations.GetLoadDataPaths("0install.net", isFile: false, resource: ["site-packages", ..interfaceUri.EscapeComponent()])
                    .SelectMany(Directory.GetDirectories)
                    .Select(dir => Path.Combine(dir, "0install", "feed.xml"))
                    .Where(File.Exists);

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

    /// <inheritdoc/>
    public void Clear()
    {
        _storeContains.Clear();
        _interfacePreferences.Clear();
        _failedFeeds.Clear();
    }
}
