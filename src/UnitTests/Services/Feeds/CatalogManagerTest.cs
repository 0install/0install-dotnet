// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Net;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds;

/// <summary>
/// Contains test methods for <see cref="CatalogManager"/>.
/// </summary>
public class CatalogManagerTest : TestWithMocksAndRedirect
{
    private readonly Config _config = new();
    private readonly Mock<ITrustManager> _trustManagerMock;
    private readonly ICatalogManager _sut;

    public CatalogManagerTest()
    {
        _trustManagerMock = CreateMock<ITrustManager>();
        _sut = new CatalogManager(_config, _trustManagerMock.Object, new SilentTaskHandler());
    }

    [Fact]
    public void GetOnline()
    {
        var catalog = CatalogTest.CreateTestCatalog();
        catalog.Normalize();

        var catalogStream = new MemoryStream();
        catalog.SaveXml(catalogStream);
        var array = catalogStream.ToArray();
        catalogStream.Position = 0;

        using var server = new MicroServer("catalog.xml", catalogStream);
        var uri = new FeedUri(server.FileUri);
        CatalogManager.SetSources(new[] {uri});
        _trustManagerMock.Setup(x => x.CheckTrust(array, uri, null)).Returns(OpenPgpUtilsTest.TestSignature);

        _sut.GetOnline().Should().Be(catalog);
    }

    [Fact]
    public void RejectDownloadInOfflineMode()
    {
        _config.NetworkUse = NetworkLevel.Offline;
        _sut.Invoking(x => x.GetOnline()).Should().Throw<WebException>();
    }

    [Fact]
    public void GetCached()
    {
        var catalog = CatalogTest.CreateTestCatalog();
        catalog.Normalize();

        _sut.GetCached().Should().BeNull();
        GetOnline();
        _sut.GetCached().Should().Be(catalog);
    }

    private static readonly FeedUri _testSource = new("http://localhost/test/");

    [Fact]
    public void AddSourceExisting()
    {
        _sut.AddSource(CatalogManager.DefaultSource).Should().BeFalse();
        CatalogManager.GetSources().Should().Equal(CatalogManager.DefaultSource);
    }

    [Fact]
    public void AddSourceNew()
    {
        _sut.AddSource(_testSource).Should().BeTrue();
        CatalogManager.GetSources().Should().Equal(CatalogManager.DefaultSource, _testSource);
    }

    [Fact]
    public void RemoveSource()
    {
        _sut.RemoveSource(CatalogManager.DefaultSource).Should().BeTrue();
        CatalogManager.GetSources().Should().BeEmpty();
    }

    [Fact]
    public void RemoveSourceMissing()
    {
        _sut.RemoveSource(_testSource).Should().BeFalse();
        CatalogManager.GetSources().Should().Equal(CatalogManager.DefaultSource);
    }

    [Fact]
    public void SetSources()
    {
        CatalogManager.SetSources(new[] {_testSource});
        CatalogManager.GetSources().Should().Equal(_testSource);
    }
}
