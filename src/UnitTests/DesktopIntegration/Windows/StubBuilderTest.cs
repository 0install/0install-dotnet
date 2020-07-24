// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Moq;
using NanoByte.Common.Native;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Store;

#if !NETFRAMEWORK
using System.IO;
using NanoByte.Common.Storage;
#endif

namespace ZeroInstall.DesktopIntegration.Windows
{
    /// <summary>
    /// Contains test methods for <see cref="StubBuilder"/>.
    /// </summary>
    public class StubBuilderTest : TestWithRedirect
    {
        private readonly StubBuilder _stubBuilder = new StubBuilder(new Mock<IIconStore>().Object);

        [SkippableFact]
        public void TestGetRunCommandLineCli()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "StubBuilder is only used on Windows");

            var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
            target.Feed.EntryPoints[0].NeedsTerminal = true;
            var commandLine = _stubBuilder.GetRunCommandLine(target);

#if NETFRAMEWORK
            commandLine.Should().HaveCount(1);
#else
            commandLine.Should().Equal(Path.Combine(Locations.InstallBase, "0install.exe"), "run", "http://example.com/test1.xml");
#endif
        }

        [SkippableFact]
        public void TestGetRunCommandLineGui()
        {
            Skip.IfNot(WindowsUtils.IsWindows, "StubBuilder is only used on Windows");

            var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
            var commandLine = _stubBuilder.GetRunCommandLine(target, "");

#if NETFRAMEWORK
            commandLine.Should().HaveCount(1);
#else
            commandLine.Should().Equal(Path.Combine(Locations.InstallBase, "0install-win.exe"), "run", "--no-wait", "http://example.com/test1.xml");
#endif
        }
    }
}
