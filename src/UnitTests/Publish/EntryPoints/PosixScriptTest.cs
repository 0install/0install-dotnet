// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// Contains test methods for <see cref="PosixScript"/>.
/// </summary>
public class PosixScriptTest : CandidateTest
{
    public static readonly PosixScript Reference = new()
    {
        RelativePath = "sh",
        Architecture = new(OS.Posix),
        Name = "sh",
        NeedsTerminal = true
    };

    [Fact]
    public void Sh() => TestAnalyze(Reference, executable: true);

    [Fact]
    public void NotExecutable()
        => new PosixScript().Analyze(baseDirectory: Directory, file: Deploy(Reference, xbit: false))
                            .Should().BeFalse();

    [Fact]
    public void NoShebang()
        => new PosixScript().Analyze(baseDirectory: Directory, file: Deploy(WindowsExeTest.Reference32, xbit: true))
                            .Should().BeFalse();
}
