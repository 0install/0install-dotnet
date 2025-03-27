// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;
using ZeroInstall.Model.Trust;
using ZeroInstall.Services.Executors;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Fetchers;
using ZeroInstall.Services.Native;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services;

/// <summary>
/// Instantiates requested services transparently on first use. Handles dependency injection internally.
/// Use exactly one instance of the service provider per user request to ensure consistent state during execution.
/// </summary>
/// <remarks>This class is thread-safe.</remarks>
public class ServiceProvider
{
    /// <summary>
    /// Creates a new service provider.
    /// </summary>
    /// <param name="handler">A callback object used when the user needs to be asked questions or informed about download and IO tasks.</param>
    /// <exception cref="IOException">There was a problem accessing a configuration file or one of the implementation stores.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file or one of the implementation stores was not permitted.</exception>
    public ServiceProvider(ITaskHandler handler)
    {
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        ImplementationStore = ImplementationStores.Default(Handler);
        _feedCache = new(() => FeedCaches.Default(OpenPgp));
        _trustManager = new(() => new(TrustDB.LoadSafe(), Config, OpenPgp, FeedCache, Handler));
        _feedManager = new(() => new(Config, FeedCache, TrustManager, Handler));
        _catalogManager = new(() => new(Config, TrustManager, Handler));
        _selectionCandidateProvider = new(() => new(Config, FeedManager, ImplementationStore, PackageManager));
        _solver = new(() =>
        {
            var backtrackingSolver = new BacktrackingSolver(SelectionCandidateProvider);
            if (Config.ExternalSolverUri == null) return backtrackingSolver;
            else
            {
                var externalSolver = new ExternalSolver(backtrackingSolver, SelectionsManager, Fetcher, Executor, FeedManager, Handler, new(Config.ExternalSolverUri) {Message = "External solver"});
                return new FallbackSolver(backtrackingSolver, externalSolver);
            }
        });
        _fetcher = new(() => new(Config, ImplementationStore, Handler));
        _executor = new(() => new(ImplementationStore));
        _selectionsManager = new(() => new(FeedManager, ImplementationStore, PackageManager));
    }

    /// <summary>
    /// A callback object used when the user needs to be asked questions or informed about download and IO tasks.
    /// </summary>
    public ITaskHandler Handler { get; }

    private readonly Lazy<Config> _config = new(Config.Load);

    /// <summary>
    /// User settings controlling network behaviour, solving, etc.
    /// </summary>
    public virtual Config Config => _config.Value;

    /// <summary>
    /// Describes an object that allows the storage and retrieval of <see cref="Implementation"/> directories.
    /// </summary>
    public IImplementationStore ImplementationStore { get; set; }

    /// <summary>
    /// Provides access to an encryption/signature system compatible with the OpenPGP standard.
    /// </summary>
    public virtual IOpenPgp OpenPgp { get; set; } = Store.Trust.OpenPgp.Verifying();

    private readonly Lazy<IFeedCache> _feedCache;

    /// <summary>
    /// Provides access to a cache of <see cref="Feed"/>s that were downloaded via HTTP(S).
    /// </summary>
    public virtual IFeedCache FeedCache => _feedCache.Value;

    private readonly Lazy<TrustManager> _trustManager;

    /// <summary>
    /// Methods for verifying signatures and user trust.
    /// </summary>
    public virtual ITrustManager TrustManager => _trustManager.Value;

    private readonly Lazy<FeedManager> _feedManager;

    /// <summary>
    /// Allows configuration of the source used to request <see cref="Feed"/>s.
    /// </summary>
    public virtual IFeedManager FeedManager => _feedManager.Value;

    private readonly Lazy<CatalogManager> _catalogManager;

    /// <summary>
    /// Provides access to remote and local <see cref="Catalog"/>s. Handles downloading, signature verification and caching.
    /// </summary>
    public virtual ICatalogManager CatalogManager => _catalogManager.Value;

    private readonly Lazy<IPackageManager> _packageManager = new(PackageManagers.Default);

    /// <summary>
    /// An external package manager that can install <see cref="PackageImplementation"/>s.
    /// </summary>
    public virtual IPackageManager PackageManager => _packageManager.Value;

    private readonly Lazy<SelectionCandidateProvider> _selectionCandidateProvider;

    /// <summary>
    /// Generates <see cref="SelectionCandidate"/>s for the <see cref="Solver"/> to choose among.
    /// </summary>
    public virtual ISelectionCandidateProvider SelectionCandidateProvider => _selectionCandidateProvider.Value;

    private readonly Lazy<ISolver> _solver;

    /// <summary>
    /// Chooses a set of <see cref="Implementation"/>s to satisfy the requirements of a program and its user.
    /// </summary>
    public virtual ISolver Solver => _solver.Value;

    private readonly Lazy<Fetcher> _fetcher;

    /// <summary>
    /// Used to download missing <see cref="Implementation"/>s.
    /// </summary>
    public virtual IFetcher Fetcher => _fetcher.Value;

    private readonly Lazy<Executor> _executor;

    /// <summary>
    /// Executes a <see cref="Selections"/> document as a program using dependency injection.
    /// </summary>
    public virtual IExecutor Executor => _executor.Value;

    private readonly Lazy<SelectionsManager> _selectionsManager;

    /// <summary>
    /// Provides methods for filtering <see cref="Selections"/>.
    /// </summary>
    public virtual ISelectionsManager SelectionsManager => _selectionsManager.Value;

    /// <summary>
    /// Tries to provide <see cref="Selections"/> that satisfy a set of <see cref="Requirements"/> without downloading any files.
    /// </summary>
    /// <param name="requirements">The requirements to satisfy.</param>
    /// <returns>The selected <see cref="ImplementationSelection"/>s or <c>null</c> if no solution was found.</returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="ArgumentException"><paramref name="requirements"/> is incomplete.</exception>
    public Selections? TrySolveOffline(Requirements requirements)
    {
        using (PropertyPointer.For(() => Config.NetworkUse).SetTemp(NetworkLevel.Offline))
            return Solver.TrySolve(requirements);
    }
}
