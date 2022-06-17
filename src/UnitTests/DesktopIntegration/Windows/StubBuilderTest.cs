// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using NanoByte.Common.Native;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Contains test methods for <see cref="StubBuilder"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public class StubBuilderTest : TestWithRedirect
{
    private readonly StubBuilder _stubBuilder = new(new Mock<IIconStore>().Object);

    public StubBuilderTest()
    {
        Skip.IfNot(WindowsUtils.IsWindows, "StubBuilder is only used on Windows");
    }

    [SkippableFact]
    public void TestGetRunCommandLineCli()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
        target.Feed.EntryPoints[0].NeedsTerminal = true;
        var commandLine = _stubBuilder.GetRunCommandLine(target);
        commandLine.Should().HaveCount(1);
    }

    [SkippableFact]
    public void TestGetRunCommandLineGui()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
        var commandLine = _stubBuilder.GetRunCommandLine(target, "");
        commandLine.Should().HaveCount(1);
    }
}
