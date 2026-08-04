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
        Assert.SkipUnless(WindowsUtils.IsWindows, "StubBuilder is only used on Windows");
    }

    [Fact]
    public void TestGetRunCommandLineCli()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
        target.Feed.EntryPoints[0].NeedsTerminal = true;

        var commandLine = _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false);

        commandLine.Should().HaveCount(1);
        using var stream = File.OpenRead(commandLine[0]);
        SHA1.Create().ComputeHash(stream)
             // Ensure deterministic generation
            .Should().Equal(0xF9, 0xA4, 0x74, 0xE3, 0x8D, 0x71, 0x63, 0x6B, 0xD8, 0x4E, 0x75, 0x07, 0x61, 0x10, 0xED, 0x36, 0xB7, 0x0E, 0x00, 0x53);
    }

    [Fact]
    public void TestGetRunCommandLineGui()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());

        var commandLine = _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false);

        commandLine.Should().HaveCount(1);
        using var stream = File.OpenRead(commandLine[0]);
        SHA1.Create().ComputeHash(stream)
             // Ensure deterministic generation
            .Should().Equal(0x44, 0xBA, 0x4D, 0x7F, 0xF0, 0xFE, 0x48, 0x16, 0x0B, 0xDE, 0x79, 0xD2, 0x9C, 0xE8, 0x55, 0x48, 0xE0, 0x48, 0x93, 0x75);
    }
}
