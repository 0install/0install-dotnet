// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Client;

/// <summary>
/// Tests for <see cref="ZeroInstallClient"/> that run <c>0install-gui</c>.
/// </summary>
public class ZeroInstallClientGuiTest : TestWithMocks
{
    private readonly Mock<IProcessLauncher> _launcherMock, _guiLauncherMock;
    private readonly ZeroInstallClient _client;

    public ZeroInstallClientGuiTest()
    {
        _launcherMock = CreateMock<IProcessLauncher>();
        _guiLauncherMock = CreateMock<IProcessLauncher>();
        _client = new(_launcherMock.Object, _guiLauncherMock.Object);
    }

    [Fact]
    public async Task Download()
    {
        var selections = SelectionsTest.CreateTestSelections();

        _guiLauncherMock.Setup(x => x.Run("download", "--batch", "--refresh", FeedTest.Test1Uri.ToStringRfc(), "--background"));

        _launcherMock.Setup(x => x.RunAndCapture(null, "select", "--batch", "--xml", "--offline", FeedTest.Test1Uri.ToStringRfc()))
                     .Returns(selections.ToXmlString());

        var result = await _client.DownloadAsync(FeedTest.Test1Uri, refresh: true);
        result.Should().Be(selections);
    }

    [Fact]
    public void Run()
    {
        _guiLauncherMock.Setup(x => x.Run("run", "--no-wait", FeedTest.Test1Uri.ToStringRfc(), "--arg"));

        _client.Run(FeedTest.Test1Uri, arguments: "--arg");
    }

    [Fact]
    public void GetRunStartInfo()
    {
        var startInfo = new ProcessStartInfo();
        _guiLauncherMock.Setup(x => x.GetStartInfo("run", "--batch", FeedTest.Test1Uri.ToStringRfc(), "--arg"))
                        .Returns(startInfo);

        _client.GetRunStartInfo(FeedTest.Test1Uri, arguments: "--arg")
               .Should().BeSameAs(startInfo);
    }
}
