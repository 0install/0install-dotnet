// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Feeds;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Contains integration tests for <see cref="AddApp"/>.
/// </summary>
[Collection("Desktop integration")]
public class AddAppTest : CliCommandTestBase<AddApp>
{
    [Fact]
    public void WithoutAlias()
    {
        GetMock<IFeedCache>().Setup(x => x.Contains(Fake.Feed1Uri)).Returns(true);
        GetMock<IFeedCache>().Setup(x => x.GetFeed(Fake.Feed1Uri)).Returns(Fake.Feed);
        if (WindowsUtils.IsWindows)
            GetMock<ICatalogManager>().Setup(x => x.GetCached()).Returns(new Catalog());

        RunAndAssert(new string[] {}, ExitCode.OK, Fake.Feed1Uri.ToStringRfc());
    }
}
