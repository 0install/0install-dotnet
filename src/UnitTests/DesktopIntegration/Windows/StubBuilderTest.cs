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

        var commandLine = _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false);

        commandLine.Should().HaveCount(1);
        using var stream = File.OpenRead(commandLine[0]);
        SHA1.Create().ComputeHash(stream)
             // Ensure deterministic generation
            .Should().Equal(0x06, 0xC3, 0xA1, 0xDC, 0x0F, 0x01, 0xCE, 0xE7, 0x78, 0xFF, 0x0C, 0x21, 0xA9, 0x19, 0xB0, 0xCA, 0x47, 0xA3, 0x55, 0x1F);
    }

    [SkippableFact]
    public void TestGetRunCommandLineGui()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());

        var commandLine = _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false);

        commandLine.Should().HaveCount(1);
        using var stream = File.OpenRead(commandLine[0]);
        SHA1.Create().ComputeHash(stream)
             // Ensure deterministic generation
            .Should().Equal(0x99, 0x16, 0x5E, 0xD7, 0x6A, 0xF9, 0x2E, 0x72, 0x0E, 0x61, 0x91, 0x15, 0x10, 0x06, 0xC4, 0x17, 0x50, 0x7B, 0x6D, 0xF3);
    }
}
