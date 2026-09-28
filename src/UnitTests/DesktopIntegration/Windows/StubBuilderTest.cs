// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using System.Runtime.Versioning;
using NanoByte.Common.Native;
using NanoByte.Common.Streams;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Contains test methods for <see cref="StubBuilder"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public class StubBuilderTest : TestWithRedirect
{
    private readonly Mock<IIconStore> _iconStoreMock = new();
    private readonly StubBuilder _stubBuilder;
    private readonly TemporaryDirectory _tempDir = new("0install-test-stub");

    public StubBuilderTest()
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, "StubBuilder is only used on Windows");
        _stubBuilder = new(_iconStoreMock.Object);
    }

    public override void Dispose()
    {
        _tempDir.Dispose();
        base.Dispose();
    }

    private const ushort SubsystemGui = 2, SubsystemCui = 3;

    [Fact]
    public void TestGetRunCommandLineCli()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
        target.Feed.EntryPoints[0].NeedsTerminal = true;

        var commandLine = _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false);

        commandLine.Should().HaveCount(1);
        string path = commandLine[0];
        Path.GetFileName(path).Should().Be("myapp.exe");
        GetSubsystem(path).Should().Be(SubsystemCui);
        StubBuilder.ReadStubData(path).Should().Equal(
            StubBuilder.TemplateRevision.ToString(CultureInfo.InvariantCulture),
            "0install.exe",
            $"run {FeedTest.Test1Uri.ToStringRfc()}",
            GetTitle(target));
        FileVersionInfo.GetVersionInfo(path).FileDescription.Should().Be(GetTitle(target));
    }

    [Fact]
    public void TestGetRunCommandLineGui()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());

        var commandLine = _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false);

        commandLine.Should().HaveCount(1);
        string path = commandLine[0];
        GetSubsystem(path).Should().Be(SubsystemGui);
        StubBuilder.ReadStubData(path).Should().Equal(
            StubBuilder.TemplateRevision.ToString(CultureInfo.InvariantCulture),
            "0install-win.exe",
            $"run --no-wait {FeedTest.Test1Uri.ToStringRfc()}",
            GetTitle(target));
    }

    [Fact]
    public void TestGetRunCommandLineOverrideNeedsTerminal()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());

        var commandLine = _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false, needsTerminal: true);

        GetSubsystem(commandLine.Single()).Should().Be(SubsystemCui);
    }

    [Fact]
    public void TestBuildRunStubDeterministic()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
        string path1 = Path.Combine(_tempDir, "1", "stub.exe");
        string path2 = Path.Combine(_tempDir, "2", "stub.exe");

        _stubBuilder.BuildRunStub(path1, target, command: null, needsTerminal: false);
        _stubBuilder.BuildRunStub(path2, target, command: null, needsTerminal: false);

        File.ReadAllBytes(path1).Should().Equal(File.ReadAllBytes(path2));
    }

    [Fact]
    public void TestBuildRunStubIcon()
    {
        string iconPath = Path.Combine(_tempDir, "icon.ico");
        typeof(IconStoreTest).CopyEmbeddedToFile("icon.ico", iconPath);
        var icon = new Icon {Href = new("http://example.com/test.ico"), MimeType = Icon.MimeTypeIco};
        _iconStoreMock.Setup(x => x.GetFresh(icon)).Returns(iconPath);
        var feed = FeedTest.CreateTestFeed();
        feed.EntryPoints[0].Icons.Add(icon);
        string path = Path.Combine(_tempDir, "stub.exe");

        _stubBuilder.BuildRunStub(path, new(FeedTest.Test1Uri, feed), command: null, needsTerminal: false);

        Win32Resources.TryRead(path, Win32Resources.TypeGroupIcon, "#32512").Should().NotBeNull();
    }

    [Fact]
    public void TestBuildRunStubInvalidIcon()
    {
        string iconPath = Path.Combine(_tempDir, "icon.ico");
        File.WriteAllText(iconPath, "not an icon");
        var icon = new Icon {Href = new("http://example.com/test.ico"), MimeType = Icon.MimeTypeIco};
        _iconStoreMock.Setup(x => x.GetFresh(icon)).Returns(iconPath);
        var feed = FeedTest.CreateTestFeed();
        feed.EntryPoints[0].Icons.Add(icon);
        string path = Path.Combine(_tempDir, "stub.exe");

        _stubBuilder.BuildRunStub(path, new(FeedTest.Test1Uri, feed), command: null, needsTerminal: false);

        StubBuilder.GetRevision(path).Should().Be(StubBuilder.TemplateRevision);
        Win32Resources.TryRead(path, Win32Resources.TypeGroupIcon, "#32512").Should().BeNull();
    }

    [Fact]
    public void TestStubLaunchesWithArguments()
    {
        string path = Path.Combine(_tempDir, "stub.exe");
        StubBuilder.BuildStub(path, exe: "cmd.exe", arguments: "/c echo embedded", title: "Test", needsTerminal: true);

        var (exitCode, output) = RunStub(path, "extra");

        exitCode.Should().Be(0);
        output.Trim().Should().Be("embedded extra");
    }

    [Fact]
    public void TestStubPassesExitCode()
    {
        string path = Path.Combine(_tempDir, "stub.exe");
        StubBuilder.BuildStub(path, exe: "cmd.exe", arguments: "/c exit", title: "Test", needsTerminal: true);

        RunStub(path, "42").exitCode.Should().Be(42);
    }

    [Fact]
    public void TestStubSpecialCharactersInTitle()
    {
        // These characters used to break compilation of stubs generated from C# source code at runtime
        const string title = "Line\rbreaks\n\u0085\u2028\u2029 \"quoted\" back\\slash";
        string path = Path.Combine(_tempDir, "stub.exe");
        StubBuilder.BuildStub(path, exe: "cmd.exe", arguments: "/c echo ok", title, needsTerminal: true);

        StubBuilder.ReadStubData(path)![3].Should().Be(title);
        FileVersionInfo.GetVersionInfo(path).FileDescription.Should().Be(title);
        RunStub(path).output.Trim().Should().Be("ok");
    }

    [Fact]
    public void TestReplacesStubFromOldVersion()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
        string path = _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false).Single();

        // Stubs compiled at runtime by older versions do not contain any stub data
        typeof(StubBuilder).CopyEmbeddedToFile("stub-gui.exe", path);
        StubBuilder.GetRevision(path).Should().Be(0);

        _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false).Should().Equal(path);
        StubBuilder.GetRevision(path).Should().Be(StubBuilder.TemplateRevision);
    }

    [Fact]
    public void TestReplacesInvalidStub()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
        string path = _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false).Single();

        File.WriteAllText(path, "not an exe");
        StubBuilder.GetRevision(path).Should().Be(0);

        _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false).Should().Equal(path);
        StubBuilder.GetRevision(path).Should().Be(StubBuilder.TemplateRevision);
    }

    [Fact]
    public void TestDoesNotReplaceReadOnlyStub()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
        string path = _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false).Single();

        typeof(StubBuilder).CopyEmbeddedToFile("stub-gui.exe", path);
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false).Should().Equal(path);
            StubBuilder.GetRevision(path).Should().Be(0);
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }

    [Fact]
    public void TestKeepsCurrentStub()
    {
        var target = new FeedTarget(FeedTest.Test1Uri, FeedTest.CreateTestFeed());
        string path = _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false).Single();
        var timestamp = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(path, timestamp);

        _stubBuilder.GetRunCommandLine(target, command: null, machineWide: false).Should().Equal(path);

        File.GetLastWriteTimeUtc(path).Should().Be(timestamp);
    }

    private static string GetTitle(FeedTarget target)
        => target.Feed.GetBestName(CultureInfo.CurrentUICulture, command: null);

    private static (int exitCode, string output) RunStub(string path, params string[] args)
    {
        var startInfo = new ProcessStartInfo(path, args.JoinEscapeArguments())
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo)!;
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }

    /// <summary>
    /// Reads the <c>Subsystem</c> field from the optional header of a PE file.
    /// </summary>
    private static ushort GetSubsystem(string path)
    {
        using var reader = new BinaryReader(File.OpenRead(path));
        reader.BaseStream.Position = 0x3C;
        int peHeaderOffset = reader.ReadInt32();
        const int signatureSize = 4, coffHeaderSize = 20, subsystemOffset = 68;
        reader.BaseStream.Position = peHeaderOffset + signatureSize + coffHeaderSize + subsystemOffset;
        return reader.ReadUInt16();
    }
}
