// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// Contains test methods for <see cref="DotNetFrameworkExe"/>.
/// </summary>
public class DotNetFrameworkExeTest : CandidateTest
{
    public static readonly DotNetFrameworkExe Reference = new()
    {
        RelativePath = "netfx.exe",
        Name = "Hello",
        Summary = "a Hello World application",
        Version = new("1.2.3.0")
    };

    public static readonly DotNetFrameworkExe Reference64 = new()
    {
        RelativePath = "netfx64.exe",
        Architecture = new(OS.All, Cpu.X64),
        Name = "Hello",
        Summary = "a Hello World application",
        Version = new("1.2.3.0")
    };

    public static readonly DotNetFrameworkExe ReferenceTerminal = new()
    {
        RelativePath = "netfx_terminal.exe",
        Name = "Hello",
        Summary = "a Hello World application",
        Version = new("1.2.3.0"),
        NeedsTerminal = true
    };

    [Fact]
    public void AnyCpu() => TestAnalyze(Reference);

    [Fact]
    public void X64() => TestAnalyze(Reference64);

    [Fact]
    public void Terminal() => TestAnalyze(ReferenceTerminal);

    [Fact]
    public void NotDotNet()
        => new DotNetFrameworkExe().Analyze(baseDirectory: Directory, file: Deploy(WindowsExeTest.Reference32, xbit: false))
                                   .Should().BeFalse();
}
