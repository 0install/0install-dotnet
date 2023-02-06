// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Contains integration tests for <see cref="List"/>.
/// </summary>
public class ListTest : CliCommandTestBase<List>
{
    private readonly Feed _feed1 = Fake.Feed, _feed2 = Fake.Feed;
    private readonly TemporaryFile _feedFile1 = new("0install-test-feed"), _feedFile2 = new("0install-test-feed");

    public ListTest()
    {
        _feed1.Uri = Fake.Feed1Uri;
        _feed2.Uri = Fake.Feed2Uri;

        var feedCacheMock = GetMock<IFeedCache>();
        feedCacheMock.SetupGet(x => x.Path).Returns("dummy");
        feedCacheMock.Setup(x => x.ListAll()).Returns(new[] {_feed1.Uri, _feed2.Uri});
        feedCacheMock.Setup(x => x.GetPath(_feed1.Uri)).Returns(_feedFile1);
        feedCacheMock.Setup(x => x.GetFeed(_feed1.Uri)).Returns(_feed1);
        feedCacheMock.Setup(x => x.GetPath(_feed2.Uri)).Returns(_feedFile2);
        feedCacheMock.Setup(x => x.GetFeed(_feed2.Uri)).Returns(_feed2);
    }

    public override void Dispose()
    {
        _feedFile1.Dispose();
        _feedFile2.Dispose();
        base.Dispose();
    }

    [Fact]
    public void TestAll()
    {
        RunAndAssert(new[]
        {
            new FeedNode(_feedFile1, _feed1),
            new FeedNode(_feedFile2, _feed2)
        }, ExitCode.OK);
    }

    [Fact]
    public void TestFiltered()
    {
        RunAndAssert(new[] {
            new FeedNode(_feedFile2, _feed2)
        }, ExitCode.OK, "test2");
    }
}
