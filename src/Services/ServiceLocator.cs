// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using JetBrains.Annotations;
using NanoByte.Common;
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
    /// <remarks>Use the property setters to override default service implementations, e.g. for mocking.</remarks>
    public class ServiceLocator
    {
        /// <summary>
        /// Creates a new service locator.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        public ServiceLocator(ITaskHandler handler)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// A callback object used when the the user needs to be asked questions or informed about download and IO tasks.
        /// </summary>
        public ITaskHandler Handler { get; }

        private Config? _config;

        /// <summary>
        /// User settings controlling network behaviour, solving, etc.
        /// </summary>
        public Config Config { get => Get(ref _config, Config.Load); set => _config = value; }

        private IImplementationStore? _implementationStore;

        /// <summary>
        /// Describes an object that allows the storage and retrieval of <see cref="Implementation"/> directories.
        /// </summary>
        public IImplementationStore ImplementationStore { get => Get(ref _implementationStore, ImplementationStores.Default); set => _implementationStore = value; }

        private IOpenPgp? _openPgp;

        /// <summary>
        /// Provides access to an encryption/signature system compatible with the OpenPGP standard.
        /// </summary>
        public IOpenPgp OpenPgp { get => Get(ref _openPgp, Store.Trust.OpenPgp.Verifying); set => _openPgp = value; }

        private IFeedCache? _feedCache;

        /// <summary>
        /// Provides access to a cache of <see cref="Feed"/>s that were downloaded via HTTP(S).
        /// </summary>
        public IFeedCache FeedCache { get => Get(ref _feedCache, () => FeedCaches.Default(OpenPgp)); set => _feedCache = value; }

        private ITrustManager? _trustManager;

        /// <summary>
        /// Methods for verifying signatures and user trust.
        /// </summary>
        public ITrustManager TrustManager { get => Get(ref _trustManager, () => new TrustManager(TrustDB.LoadSafe(), Config, OpenPgp, FeedCache, Handler)); set => _trustManager = value; }

        private IFeedManager? _feedManager;

        /// <summary>
        /// Allows configuration of the source used to request <see cref="Feed"/>s.
        /// </summary>
        public IFeedManager FeedManager { get => Get(ref _feedManager, () => new FeedManager(Config, FeedCache, TrustManager, Handler)); set => _feedManager = value; }

        private ICatalogManager? _catalogManager;

        /// <summary>
        /// Provides access to remote and local <see cref="Catalog"/>s. Handles downloading, signature verification and caching.
        /// </summary>
        public ICatalogManager CatalogManager { get => Get(ref _catalogManager, () => new CatalogManager(TrustManager, Handler)); set => _catalogManager = value; }

        private IPackageManager? _packageManager;

        /// <summary>
        /// An external package manager that can install <see cref="PackageImplementation"/>s.
        /// </summary>
        public IPackageManager PackageManager { get => Get(ref _packageManager, PackageManagers.Default); set => _packageManager = value; }

        private ISelectionCandidateProvider? _selectionCandidateProvider;

        /// <summary>
        /// Generates <see cref="SelectionCandidate"/>s for the <see cref="Solver"/> to choose among.
        /// </summary>
        public ISelectionCandidateProvider SelectionCandidateProvider { get => Get(ref _selectionCandidateProvider, () => new SelectionCandidateProvider(Config, FeedManager, ImplementationStore, PackageManager)); set => _selectionCandidateProvider = value; }

        private ISolver? _solver;

        /// <summary>
        /// Chooses a set of <see cref="Implementation"/>s to satisfy the requirements of a program and its user.
        /// </summary>
        public ISolver Solver
        {
            get => Get(ref _solver, () =>
            {
                var backtrackingSolver = new BacktrackingSolver(SelectionCandidateProvider);
                if (Config.ExternalSolverUri == null) return backtrackingSolver;
                else
                {
                    var externalSolver = new ExternalSolver(backtrackingSolver, SelectionsManager, Fetcher, Executor, Config.ExternalSolverUri, FeedManager, Handler);
                    return new FallbackSolver(backtrackingSolver, externalSolver);
                }
            });
            set => _solver = value;
        }

        private IFetcher? _fetcher;

        /// <summary>
        /// Used to download missing <see cref="Implementation"/>s.
        /// </summary>
        public IFetcher Fetcher { get => Get(ref _fetcher, () => new Fetcher(Config, ImplementationStore, Handler)); set => _fetcher = value; }

        private IExecutor? _executor;

        /// <summary>
        /// Executes a <see cref="Selections"/> document as a program using dependency injection.
        /// </summary>
        public IExecutor Executor { get => Get(ref _executor, () => new Executor(ImplementationStore)); set => _executor = value; }

        private ISelectionsManager? _selectionsManager;

        /// <summary>
        /// Provides methods for filtering <see cref="Selections"/>.
        /// </summary>
        public ISelectionsManager SelectionsManager { get => Get(ref _selectionsManager, () => _selectionsManager = new SelectionsManager(FeedManager, ImplementationStore, PackageManager)); set => _selectionsManager = value; }

        private static T Get<T>(ref T? value, [InstantHandle] Func<T> build) where T : class
        {
            if (value == null)
            {
                value = build();
                Log.Debug("Initialized by Service Locator: " + value);
            }

            return value;
        }
    }
}
