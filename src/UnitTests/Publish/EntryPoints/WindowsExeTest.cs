// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// Contains test methods for <see cref="WindowsExe"/>.
/// </summary>
public class WindowsExeTest : CandidateTest
{
    public static readonly WindowsExe Reference32 = new()
    {
        RelativePath = "windows32.exe",
        Architecture = new(OS.Windows),
        Name = "Hello",
        Summary = "a Hello World application",
        Version = new("1.2.3.0")
    };

    public static readonly WindowsExe Reference64 = new()
    {
        RelativePath = "windows64.exe",
        Architecture = new(OS.Windows, Cpu.X64),
        Name = "Hello",
        Summary = "a Hello World application",
        Version = new("1.2.3.0")
    };

    public static readonly WindowsExe ReferenceTerminal = new()
    {
        RelativePath = "windows32_terminal.exe",
        Architecture = new(OS.Windows),
        Name = "Hello",
        Summary = "a Hello World application",
        Version = new("1.2.3.0"),
        NeedsTerminal = true
    };

    public WindowsExeTest()
    {
        Assert.SkipUnless(WindowsUtils.IsWindows, reason: "Non-Windows systems cannot parse PE headers.");
    }

    [Fact]
    public void X86() => TestAnalyze(Reference32);

    [Fact]
    public void X64() => TestAnalyze(Reference64);

    [Fact]
    public void X86Terminal() => TestAnalyze(ReferenceTerminal);

    [Fact]
    public void NotExe()
        => new WindowsExe().Analyze(baseDirectory: Directory, file: Deploy(PosixScriptTest.Reference, xbit: false))
                           .Should().BeFalse();
}
