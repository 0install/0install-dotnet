// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;
using ZeroInstall.Model;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// Contains test methods for <see cref="DotNetExe"/>.
    /// </summary>
    public class DotNetExeTest : CandidateTest
    {
        public static readonly DotNetExe Reference = new()
        {
            RelativePath = "dotnet.exe",
            Architecture = new(OS.Windows, Cpu.All),
            Name = "Hello",
            Summary = "a Hello World application",
            Version = new("1.2.3.0")
        };

        public static readonly DotNetExe Reference64 = new()
        {
            RelativePath = "dotnet64.exe",
            Architecture = new(OS.Windows, Cpu.X64),
            Name = "Hello",
            Summary = "a Hello World application",
            Version = new("1.2.3.0")
        };

        public static readonly DotNetExe ReferenceTerminal = new()
        {
            RelativePath = "dotnet_terminal.exe",
            Architecture = new(OS.Windows, Cpu.All),
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
            => new DotNetExe().Analyze(baseDirectory: Directory, file: Deploy(WindowsExeTest.Reference32, xbit: false))
                              .Should().BeFalse();
    }
}
