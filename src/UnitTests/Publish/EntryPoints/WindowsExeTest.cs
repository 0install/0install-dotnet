// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using NanoByte.Common.Native;
using Xunit;
using ZeroInstall.Model;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// Contains test methods for <see cref="WindowsExe"/>.
    /// </summary>
    public class WindowsExeTest : CandidateTest
    {
        public static readonly WindowsExe Reference32 = new()
        {
            RelativePath = "windows32.exe",
            Architecture = new Architecture(OS.Windows, Cpu.All),
            Name = "Hello",
            Summary = "a Hello World application",
            Version = new ImplementationVersion("1.2.3.0")
        };

        public static readonly WindowsExe Reference64 = new()
        {
            RelativePath = "windows64.exe",
            Architecture = new Architecture(OS.Windows, Cpu.X64),
            Name = "Hello",
            Summary = "a Hello World application",
            Version = new ImplementationVersion("1.2.3.0")
        };

        public static readonly WindowsExe ReferenceTerminal = new()
        {
            RelativePath = "windows32_terminal.exe",
            Architecture = new Architecture(OS.Windows, Cpu.All),
            Name = "Hello",
            Summary = "a Hello World application",
            Version = new ImplementationVersion("1.2.3.0"),
            NeedsTerminal = true
        };

        [SkippableFact]
        public void X86()
        {
            Skip.IfNot(WindowsUtils.IsWindows, reason: "Non-Windows systems cannot parse PE headers.");
            TestAnalyze(Reference32);
        }

        [SkippableFact]
        public void X64()
        {
            Skip.IfNot(WindowsUtils.IsWindows, reason: "Non-Windows systems cannot parse PE headers.");
            TestAnalyze(Reference64);
        }

        [SkippableFact]
        public void X86Terminal()
        {
            Skip.IfNot(WindowsUtils.IsWindows, reason: "Non-Windows systems cannot parse PE headers.");
            TestAnalyze(ReferenceTerminal);
        }

        [SkippableFact]
        public void NotExe()
        {
            Skip.IfNot(WindowsUtils.IsWindows, reason: "Non-Windows systems cannot parse PE headers.");
            new WindowsExe().Analyze(baseDirectory: Directory, file: Deploy(PosixScriptTest.Reference, xbit: false))
                            .Should().BeFalse();
        }
    }
}
