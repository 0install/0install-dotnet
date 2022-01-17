// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Streams;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Client;

/// <summary>
/// Tests for <see cref="ZeroInstallClient"/> that run <c>0install</c>.
/// </summary>
public class ZeroInstallClientTest : TestWithMocks
{
    private readonly Mock<ISubProcess> _subProcessMock;
    private readonly Mock<IProcessLauncher> _launcherMock;
    private readonly ZeroInstallClient _client;

    public ZeroInstallClientTest()
    {
        _subProcessMock = CreateMock<ISubProcess>();
        _launcherMock = CreateMock<IProcessLauncher>();
        _client = new(_subProcessMock.Object, _launcherMock.Object);
    }

    [Fact]
    public async Task Select()
    {
        var selections = SelectionsTest.CreateTestSelections();

        _subProcessMock.Setup(x => x.Run(null, "select", "--batch", "--xml", FeedTest.Test1Uri.ToStringRfc()))
                       .Returns(selections.ToXmlString());

        var result = await _client.SelectAsync(FeedTest.Test1Uri);
        result.Should().Be(selections);
    }

    [Fact]
    public async Task Download()
    {
        var selections = SelectionsTest.CreateTestSelections();

        _subProcessMock.Setup(x => x.Run(null, "download", "--batch", "--refresh", FeedTest.Test1Uri.ToStringRfc(), "--xml"))
                       .Returns(selections.ToXmlString());

        var result = await _client.DownloadAsync(FeedTest.Test1Uri, refresh: true);
        result.Should().Be(selections);
    }

    [Fact]
    public void Run()
    {
        _launcherMock.Setup(x => x.Start("run", "--no-wait", FeedTest.Test1Uri.ToStringRfc(), "--arg"))
                     .Returns(new Process());

        _client.Run(FeedTest.Test1Uri, arguments: "--arg");
    }

    [Fact]
    public void RunAndWait()
    {
        var process = new Process();
        _launcherMock.Setup(x => x.Start("run", FeedTest.Test1Uri.ToStringRfc(), "--arg"))
                     .Returns(process);

        _client.RunWithProcess(FeedTest.Test1Uri, arguments: "--arg")
               .Should().BeSameAs(process);
    }

    [Fact]
    public async Task GetIntegrationAsync()
    {
        var appList = AppListTest.CreateTestAppListWithAPs();

        _subProcessMock.Setup(x => x.Run(null, "list-apps", "--batch", "--xml", FeedTest.Test1Uri.ToStringRfc()))
                       .Returns(appList.ToXmlString());

        var result = await _client.GetIntegrationAsync(FeedTest.Test1Uri);
        result.Should().Contain("alias", "auto-start", "capability-registration");
    }

    [Fact]
    public async Task Integrate()
    {
        _subProcessMock.Setup(x => x.Run(null, "integrate", "--batch", FeedTest.Test1Uri.ToStringRfc(),
                            "--add", "capability-registration",
                            "--remove", "alias", "--remove", "auto-start"))
                       .Returns("");

        await _client.IntegrateAsync(FeedTest.Test1Uri,
            add: new[] { "capability-registration" },
            remove: new[] { "alias", "auto-start" });
    }

    [Fact]
    public async Task Remove()
    {
        _subProcessMock.Setup(x => x.Run(null, "remove", "--batch", FeedTest.Test1Uri.ToStringRfc()))
                       .Returns("");

        await _client.RemoveAsync(FeedTest.Test1Uri);
    }

    [Fact]
    public async Task Fetch()
    {
        _subProcessMock.Setup(x => x.Run(It.IsAny<Action<StreamWriter>>(), "fetch"))
                       .Callback((Action<StreamWriter> callback, string[] _) =>
                        {
                            var stream = new MemoryStream();
                            var writer = new StreamWriter(stream);
                            callback(writer);
                            writer.Flush();
                            stream.ReadToString().Should().StartWith("<?xml version=\"1.0\"?><interface");
                        })
                       .Returns("");

        var implementation = ImplementationTest.CreateTestImplementation();
        await _client.FetchAsync(implementation);
    }
}
