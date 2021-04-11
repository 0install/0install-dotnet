// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Executors;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Fetchers;
using ZeroInstall.Services.Native;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services
{
    /// <summary>
    /// Instantiates requested services transparently on first use. Handles dependency injection internally.
    /// Use exactly one instance of the service locator per user request to ensure consistent state during execution.
    /// </summary>
    /// <remarks>This class is thread-safe.</remarks>
    public class ServiceLocator
    {
        /// <summary>
        /// Creates a new service locator.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        public ServiceLocator(ITaskHandler handler)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _feedCache = new(() => FeedCaches.Default(OpenPgp));
            _trustManager = new(() => new TrustManager(TrustDB.LoadSafe(), Config, OpenPgp, FeedCache, Handler));
            _feedManager = new(() => new FeedManager(Config, FeedCache, TrustManager, Handler));
            _catalogManager = new(() => new CatalogManager(TrustManager, Handler));
            _selectionCandidateProvider = new(() => new SelectionCandidateProvider(Config, FeedManager, ImplementationStore, PackageManager));
            _solver = new(() =>
            {
                var backtrackingSolver = new BacktrackingSolver(SelectionCandidateProvider);
                if (Config.ExternalSolverUri == null) return backtrackingSolver;
                else
                {
                    var externalSolver = new ExternalSolver(backtrackingSolver, SelectionsManager, Fetcher, Executor, Config.ExternalSolverUri, FeedManager, Handler);
                    return new FallbackSolver(backtrackingSolver, externalSolver);
                }
            });
            _fetcher = new(() => new Fetcher(Config, ImplementationStore, Handler));
            _executor = new(() => new Executor(ImplementationStore));
            _selectionsManager = new(() => new SelectionsManager(FeedManager, ImplementationStore, PackageManager));
        }

        /// <summary>
        /// A callback object used when the the user needs to be asked questions or informed about download and IO tasks.
        /// </summary>
        public virtual ITaskHandler Handler { get; }

        private readonly Lazy<Config> _config = new(Config.Load);

        /// <summary>
        /// User settings controlling network behaviour, solving, etc.
        /// </summary>
        public virtual Config Config => _config.Value;

        /// <summary>
        /// Describes an object that allows the storage and retrieval of <see cref="Implementation"/> directories.
        /// </summary>
        public virtual IImplementationStore ImplementationStore { get; set; } = ImplementationStores.Default();

        /// <summary>
        /// Provides access to an encryption/signature system compatible with the OpenPGP standard.
        /// </summary>
        public virtual IOpenPgp OpenPgp { get; set; } = Store.Trust.OpenPgp.Verifying();

        private readonly Lazy<IFeedCache> _feedCache;

        /// <summary>
        /// Provides access to a cache of <see cref="Feed"/>s that were downloaded via HTTP(S).
        /// </summary>
        public virtual IFeedCache FeedCache => _feedCache.Value;

        private readonly Lazy<ITrustManager> _trustManager;

        /// <summary>
        /// Methods for verifying signatures and user trust.
        /// </summary>
        public virtual ITrustManager TrustManager => _trustManager.Value;

        private readonly Lazy<IFeedManager> _feedManager;

        /// <summary>
        /// Allows configuration of the source used to request <see cref="Feed"/>s.
        /// </summary>
        public virtual IFeedManager FeedManager => _feedManager.Value;

        private readonly Lazy<ICatalogManager> _catalogManager;

        /// <summary>
        /// Provides access to remote and local <see cref="Catalog"/>s. Handles downloading, signature verification and caching.
        /// </summary>
        public virtual ICatalogManager CatalogManager => _catalogManager.Value;

        private readonly Lazy<IPackageManager> _packageManager = new(PackageManagers.Default);

        /// <summary>
        /// An external package manager that can install <see cref="PackageImplementation"/>s.
        /// </summary>
        public virtual IPackageManager PackageManager => _packageManager.Value;

        private readonly Lazy<ISelectionCandidateProvider> _selectionCandidateProvider;

        /// <summary>
        /// Generates <see cref="SelectionCandidate"/>s for the <see cref="Solver"/> to choose among.
        /// </summary>
        public virtual ISelectionCandidateProvider SelectionCandidateProvider => _selectionCandidateProvider.Value;

        private readonly Lazy<ISolver> _solver;

        /// <summary>
        /// Chooses a set of <see cref="Implementation"/>s to satisfy the requirements of a program and its user.
        /// </summary>
        public virtual ISolver Solver => _solver.Value;

        private readonly Lazy<IFetcher> _fetcher;

        /// <summary>
        /// Used to download missing <see cref="Implementation"/>s.
        /// </summary>
        public virtual IFetcher Fetcher => _fetcher.Value;

        private readonly Lazy<IExecutor> _executor;

        /// <summary>
        /// Executes a <see cref="Selections"/> document as a program using dependency injection.
        /// </summary>
        public virtual IExecutor Executor => _executor.Value;

        private readonly Lazy<SelectionsManager> _selectionsManager;

        /// <summary>
        /// Provides methods for filtering <see cref="Selections"/>.
        /// </summary>
        public virtual ISelectionsManager SelectionsManager => _selectionsManager.Value;
    }
}
