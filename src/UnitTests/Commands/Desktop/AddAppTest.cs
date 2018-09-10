// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Xunit;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Commands.Desktop
{
    /// <summary>
    /// Contains integration tests for <see cref="AddApp"/>.
    /// </summary>
    public class AddAppTest : CommandTestBase<AddApp>
    {
        [Fact]
        public void TestWithoutAlias()
        {
            GetMock<IFeedCache>().Setup(x => x.Contains(Fake.Feed1Uri)).Returns(true);
            GetMock<IFeedCache>().Setup(x => x.GetFeed(Fake.Feed1Uri)).Returns(Fake.Feed);
            GetMock<ICatalogManager>().Setup(x => x.GetCached()).Returns(new Catalog());

            RunAndAssert(new string[] {}, ExitCode.OK, Fake.Feed1Uri.ToStringRfc());
        }
    }
}
