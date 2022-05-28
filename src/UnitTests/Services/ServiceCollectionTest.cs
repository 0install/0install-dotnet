// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NET
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZeroInstall.Services.Executors;
using ZeroInstall.Services.Fetchers;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Services;

public class ServiceCollectionTest
{
    [Fact]
    public void RegistersServices()
    {
        var provider = new ServiceCollection()
                      .AddZeroInstall()
                      .BuildServiceProvider();
        provider.GetRequiredService<ISolver>();
        provider.GetRequiredService<IFetcher>();
        provider.GetRequiredService<IExecutor>();
    }

    [Fact]
    public void BindsConfiguration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["NetworkUse"] = "Minimal",
            ["HelpWithTesting"] = "true",
            ["SyncServer"] = "http://example.com"
        }).Build();

        var config = new ServiceCollection()
                    .AddZeroInstall(configuration)
                    .BuildServiceProvider().
                     GetRequiredService<Config>();

        config.NetworkUse.Should().Be(NetworkLevel.Minimal);
        config.HelpWithTesting.Should().BeTrue();
        config.SyncServer.Should().Be(new FeedUri("http://example.com"));

    }
}
#endif
