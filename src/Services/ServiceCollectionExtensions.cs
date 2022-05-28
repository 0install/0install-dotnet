// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NET
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NanoByte.Common.Net;
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
/// Provides extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a set of scoped services for using Zero Install functionality.
    /// </summary>
    /// <typeparam name="TTaskHandler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</typeparam>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configuration">An optional configuration source for building <see cref="Config"/> instead of the default config files.</param>
    public static IServiceCollection AddZeroInstall<TTaskHandler>(this IServiceCollection services, IConfiguration? configuration = null)
        where TTaskHandler : class, ITaskHandler
        => services.AddScoped<ITaskHandler, TTaskHandler>()
                   .AddScoped(_ => configuration?.Get<Config>() ?? Config.Load())
                   .AddScoped(_ => ImplementationStores.Default())
                   .AddScoped(_ => OpenPgp.Verifying())
                   .AddScoped(provider => FeedCaches.Default(provider.GetRequiredService<IOpenPgp>()))
                   .AddScoped(_ => TrustDB.LoadSafe())
                   .AddScoped<ITrustManager, TrustManager>()
                   .AddScoped<IFeedManager, FeedManager>()
                   .AddScoped<ICatalogManager, CatalogManager>()
                   .AddScoped(_ => PackageManagers.Default())
                   .AddScoped<ISelectionsManager, SelectionsManager>()
                   .AddScoped<ISolver, BacktrackingSolver>()
                   .AddScoped<IFetcher, Fetcher>()
                   .AddScoped<IExecutor, Executor>()
                   .AddScoped<ISelectionCandidateProvider, SelectionCandidateProvider>();

    /// <summary>
    /// Registers a set of scoped services for using Zero Install functionality.
    /// Automatically uses <see cref="ILogger{TCategoryName}"/> and <see cref="ICredentialProvider"/> if registered in <paramref name="services"/>.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configuration">An optional configuration source for building <see cref="Config"/> instead of the default config files.</param>
    /// <seealso cref="ConfigurationCredentialProviderRegistration.ConfigureCredentials"/>
    public static IServiceCollection AddZeroInstall(this IServiceCollection services, IConfiguration? configuration = null)
        => services.AddZeroInstall<ServiceTaskHandler>(configuration);
}
#endif
