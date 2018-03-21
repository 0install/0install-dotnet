/*
 * Copyright 2010-2016 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using FluentAssertions;
using NanoByte.Common.Native;
using Xunit;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// Contains test methods for <see cref="WindowsExe"/>.
    /// </summary>
    public class WindowsExeTest : CandidateTest
    {
        public static readonly WindowsExe Reference32 = new WindowsExe
        {
            RelativePath = "windows32.exe",
            Architecture = new Architecture(OS.Windows, Cpu.All),
            Name = "Hello",
            Summary = "a Hello World application",
            Version = new ImplementationVersion("1.2.3.0")
        };

        public static readonly WindowsExe Reference64 = new WindowsExe
        {
            RelativePath = "windows64.exe",
            Architecture = new Architecture(OS.Windows, Cpu.X64),
            Name = "Hello",
            Summary = "a Hello World application",
            Version = new ImplementationVersion("1.2.3.0")
        };

        public static readonly WindowsExe ReferenceTerminal = new WindowsExe
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
