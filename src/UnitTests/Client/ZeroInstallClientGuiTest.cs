// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Client
{
    /// <summary>
    /// Tests for <see cref="ZeroInstallClient"/> that run <c>0install-gui</c>.
    /// </summary>
    public class ZeroInstallClientGuiTest : TestWithMocks
    {
        private readonly Mock<ISubProcess> _subProcessMock;
        private readonly Mock<IProcessLauncher> _guiLauncherMock;
        private readonly ZeroInstallClient _client;

        public ZeroInstallClientGuiTest()
        {
            _subProcessMock = CreateMock<ISubProcess>();
            _guiLauncherMock = CreateMock<IProcessLauncher>();
            _client = new(_subProcessMock.Object, CreateMock<IProcessLauncher>().Object, _guiLauncherMock.Object);
        }

        [Fact]
        public async Task Download()
        {
            var selections = SelectionsTest.CreateTestSelections();

            _guiLauncherMock.Setup(x => x.Run("download", "--batch", "--refresh", FeedTest.Test1Uri.ToStringRfc(), "--background"))
                            .Returns(0);

            _subProcessMock.Setup(x => x.Run(null, "select", "--batch", "--xml", "--offline", FeedTest.Test1Uri.ToStringRfc()))
                           .Returns(selections.ToXmlString());

            var result = await _client.DownloadAsync(FeedTest.Test1Uri, refresh: true);
            result.Should().Be(selections);
        }

        [Fact]
        public void Run()
        {
            _guiLauncherMock.Setup(x => x.Start("run", "--no-wait", FeedTest.Test1Uri.ToStringRfc(), "--arg"))
                            .Returns(new Process());

            _client.Run(FeedTest.Test1Uri, arguments: "--arg");
        }

        [Fact]
        public void RunAndWait()
        {
            var process = new Process();
            _guiLauncherMock.Setup(x => x.Start("run", FeedTest.Test1Uri.ToStringRfc(), "--arg"))
                            .Returns(process);

            _client.RunWithProcess(FeedTest.Test1Uri, arguments: "--arg")
                   .Should().BeSameAs(process);
        }
    }
}
