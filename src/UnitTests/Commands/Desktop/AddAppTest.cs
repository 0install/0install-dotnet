// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Feeds;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Contains integration tests for <see cref="AddApp"/>.
/// </summary>
[Collection("Desktop integration")]
public class AddAppTest : CliCommandTestBase<AddApp>
{
    private Mock<IFeedCache> FeedCache => GetMock<IFeedCache>();
    private Mock<ICatalogManager> CatalogManagerMock => GetMock<ICatalogManager>();

    [Fact]
    public void WithoutAlias()
    {
        CatalogManagerMock.Setup(x => x.TryGetCached()).Returns(new Catalog());
        FeedCache.Setup(x => x.GetFeed(Fake.Feed1Uri)).Returns(Fake.Feed);

        RunAndAssert(null, ExitCode.OK, Fake.Feed1Uri.ToStringRfc());
    }

    [Fact]
    public void KioskModeOK()
    {
        Sut.Config.KioskMode = true;
        Catalog catalog = new() {Feeds = {new() {Uri = Fake.Feed1Uri, Name = "MyApp"}}};
        CatalogManagerMock.Setup(x => x.TryGetCached()).Returns(catalog);

        FeedCache.Setup(x => x.GetFeed(Fake.Feed1Uri)).Returns(Fake.Feed);

        RunAndAssert(null, ExitCode.OK, Fake.Feed1Uri.ToStringRfc());
    }

    [Fact]
    public void KioskModeReject()
    {
        Sut.Config.KioskMode = true;
        Catalog catalog = new();
        CatalogManagerMock.Setup(x => x.TryGetCached()).Returns(catalog);
        CatalogManagerMock.Setup(x => x.GetOnline()).Returns(catalog);

        Sut.Parse([Fake.Feed1Uri.ToStringRfc()]);
        Assert.Throws<WebException>(() => Sut.Execute());
    }
}
