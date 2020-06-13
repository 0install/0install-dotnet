// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;
using ZeroInstall.Model;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// Contains test methods for <see cref="PosixBinary"/>.
    /// </summary>
    public class PosixBinaryTest : CandidateTest
    {
        public static readonly PosixBinary Reference32 = new PosixBinary {RelativePath = "elf32", Name = "elf32", Architecture = new Architecture(OS.Linux, Cpu.I386)};
        public static readonly PosixBinary Reference64 = new PosixBinary {RelativePath = "elf64", Name = "elf64", Architecture = new Architecture(OS.Linux, Cpu.X64)};

        [Fact]
        public void Elf32() => TestAnalyze(Reference32, executable: true);

        [Fact]
        public void Elf64() => TestAnalyze(Reference64, executable: true);

        [Fact]
        public void NotExecutable()
            => new PosixBinary().Analyze(baseDirectory: Directory, file: Deploy(Reference32, xbit: false))
                                .Should().BeFalse();

        [Fact]
        public void NotElf()
            => new PosixBinary().Analyze(baseDirectory: Directory, file: Deploy(PosixScriptTest.Reference, xbit: true))
                                .Should().BeFalse();
    }
}
