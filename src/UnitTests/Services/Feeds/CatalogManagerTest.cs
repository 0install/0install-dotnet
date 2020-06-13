// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using FluentAssertions;
using Moq;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Services.Feeds
{
    /// <summary>
    /// Contains test methods for <see cref="CatalogManager"/>.
    /// </summary>
    public class CatalogManagerTest : TestWithMocksAndRedirect
    {
        private readonly Mock<ITrustManager> _trustManagerMock;
        private readonly ICatalogManager _sut;

        public CatalogManagerTest()
        {
            _trustManagerMock = CreateMock<ITrustManager>();
            _sut = new CatalogManager(_trustManagerMock.Object, new SilentTaskHandler());
        }

        [Fact]
        public void TestGetOnline()
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
        public void TestGetCached()
        {
            var catalog = CatalogTest.CreateTestCatalog();
            catalog.Normalize();

            _sut.GetCached().Should().BeNull();
            TestGetOnline();
            _sut.GetCached().Should().Be(catalog);
        }

        private static readonly FeedUri _testSource = new FeedUri("http://localhost/test/");

        [Fact]
        public void TestAddSourceExisting()
        {
            _sut.AddSource(CatalogManager.DefaultSource).Should().BeFalse();
            CatalogManager.GetSources().Should().Equal(CatalogManager.DefaultSource);
        }

        [Fact]
        public void TestAddSourceNew()
        {
            _sut.AddSource(_testSource).Should().BeTrue();
            CatalogManager.GetSources().Should().Equal(CatalogManager.DefaultSource, _testSource);
        }

        [Fact]
        public void TestRemoveSource()
        {
            _sut.RemoveSource(CatalogManager.DefaultSource).Should().BeTrue();
            CatalogManager.GetSources().Should().BeEmpty();
        }

        [Fact]
        public void TestRemoveSourceMissing()
        {
            _sut.RemoveSource(_testSource).Should().BeFalse();
            CatalogManager.GetSources().Should().Equal(CatalogManager.DefaultSource);
        }

        [Fact]
        public void TestSetSources()
        {
            CatalogManager.SetSources(new[] {_testSource});
            CatalogManager.GetSources().Should().Equal(_testSource);
        }
    }
}
