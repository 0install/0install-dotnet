// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using FluentAssertions;
using Moq;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Store;

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains test methods for <see cref="StubBuilder"/>.
    /// </summary>
    public class StubBuilderTest : TestWithRedirect
    {
        private readonly StubBuilder _stubBuilder = new StubBuilder(new Mock<IIconStore>().Object);

        [Fact]
        public void TestGetRunCommandLineCli()
        {
            var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
            target.Feed.EntryPoints[0].NeedsTerminal = true;
            var commandLine = _stubBuilder.GetRunCommandLine(target);

            if (WindowsUtils.IsWindows)
            {
                commandLine.Should().HaveCount(1);
            }
            else
            {
                commandLine.Should().Equal(Path.Combine(Locations.InstallBase, "0install.exe"), "run", "http://example.com/test1.xml");
            }
        }

        [Fact]
        public void TestGetRunCommandLineGui()
        {
            var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
            var commandLine = _stubBuilder.GetRunCommandLine(target, "");

            if (WindowsUtils.IsWindows)
            {
                commandLine.Should().HaveCount(1);
            }
            else
            {
                commandLine.Should().Equal(Path.Combine(Locations.InstallBase, "0install-win.exe"), "run", "--no-wait", "http://example.com/test1.xml");
            }
        }
    }
}
