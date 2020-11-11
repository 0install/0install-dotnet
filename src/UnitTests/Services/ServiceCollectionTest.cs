// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NET
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZeroInstall.Services.Executors;
using ZeroInstall.Services.Fetchers;
using ZeroInstall.Services.Solvers;

namespace ZeroInstall.Services
{
    public class ServiceCollectionTest
    {
        [Fact]
        public void TestDependencyResolution()
        {
            var provider = new ServiceCollection().AddZeroInstall().BuildServiceProvider();
            provider.GetRequiredService<ISolver>();
            provider.GetRequiredService<IFetcher>();
            provider.GetRequiredService<IExecutor>();
        }
    }
}
#endif
