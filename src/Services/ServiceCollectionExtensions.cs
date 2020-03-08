// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NanoByte.Common.Net;
using NanoByte.Common.Tasks;
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
    /// Provides extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a set of scoped services for using Zero Install functionality.
        /// </summary>
        /// <typeparam name="TTaskHandler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</typeparam>
        /// <param name="services">The service collection to add the services to.</param>
        public static IServiceCollection AddZeroInstall<TTaskHandler>(this IServiceCollection services)
            where TTaskHandler : class, ITaskHandler
            => services.AddScoped<ITaskHandler, TTaskHandler>()
                       .AddScoped(x => Config.Load())
                       .AddScoped(x => ImplementationStores.Default())
                       .AddScoped(x => OpenPgp.Verifying())
                       .AddScoped(x => FeedCaches.Default(x.GetService<IOpenPgp>()))
                       .AddScoped(x => TrustDB.LoadSafe())
                       .AddScoped<ITrustManager, TrustManager>()
                       .AddScoped<IFeedManager, FeedManager>()
                       .AddScoped<ICatalogManager, CatalogManager>()
                       .AddScoped(x => PackageManagers.Default())
                       .AddScoped<ISelectionsManager, SelectionsManager>()
                       .AddScoped<ISolver, BacktrackingSolver>()
                       .AddScoped<IFetcher, SequentialFetcher>()
                       .AddScoped<IExecutor, Executor>()
                       .AddScoped<ISelectionCandidateProvider, SelectionCandidateProvider>();

        /// <summary>
        /// Registers a set of scoped services for using Zero Install functionality.
        /// Automatically uses <see cref="ILogger{TCategoryName}"/> and <see cref="ICredentialProvider"/> if registered in <paramref name="services"/>.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        /// <seealso cref="ConfigurationCredentialProviderRegisration.ConfigureCredentials"/>
        public static IServiceCollection AddZeroInstall(this IServiceCollection services)
            => services.AddZeroInstall<ServiceTaskHandler>();
    }
}
#endif
