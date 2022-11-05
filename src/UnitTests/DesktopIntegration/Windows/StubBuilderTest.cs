// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using System.Security.Cryptography;
using NanoByte.Common.Native;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Contains test methods for <see cref="StubBuilder"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public class StubBuilderTest : TestWithRedirect
{
    private readonly StubBuilder _stubBuilder = new(Mock.Of<IIconStore>());

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
        using var stream = File.OpenRead(commandLine[0]);
        SHA1.Create().ComputeHash(stream)
             // Ensure deterministic generation
            .Should().Equal(0x0C, 0x01, 0xBF, 0xB8, 0xC3, 0x15, 0x32, 0x62, 0x1C, 0xF1, 0x91, 0x78, 0x4D, 0xDC, 0xC7, 0x11, 0x50, 0x04, 0x81, 0xBD);
    }

    [SkippableFact]
    public void TestGetRunCommandLineGui()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());

        var commandLine = _stubBuilder.GetRunCommandLine(target);

        commandLine.Should().HaveCount(1);
        using var stream = File.OpenRead(commandLine[0]);
        SHA1.Create().ComputeHash(stream)
             // Ensure deterministic generation
            .Should().Equal(0x0F, 0xCB, 0x80, 0xA7, 0x95, 0x2C, 0xBA, 0xA7, 0xC6, 0xB8, 0x5F, 0xFE, 0xE3, 0x3B, 0x6C, 0x1D, 0x9E, 0x6A, 0x43, 0x80);
    }
}
