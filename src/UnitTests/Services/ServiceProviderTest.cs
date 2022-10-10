// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Services;

public class ServiceProviderTest : TestWithRedirect
{
    private readonly ServiceProvider _provider = new(new SilentTaskHandler());

    [Fact]
    public void SolveOfflineFail()
    {
        _provider.TrySolveOffline(FeedTest.Test1Uri)
                 .Should().BeNull();
    }
}
