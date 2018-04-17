// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Contains test methods for <see cref="Architecture"/>.
    /// </summary>
    public class ArchitectureTest
    {
        [Fact]
        public void TestConstructor()
        {
            new Architecture("*-*").Should().Be(new Architecture(OS.All, Cpu.All));
            new Architecture("Linux-*").Should().Be(new Architecture(OS.Linux, Cpu.All));
            new Architecture("*-i686").Should().Be(new Architecture(OS.All, Cpu.I686));
            new Architecture("Linux-i686").Should().Be(new Architecture(OS.Linux, Cpu.I686));
        }

        [Fact]
        public void TestIsCompatible()
        {
            new Architecture(OS.Windows, Cpu.I486).IsCompatible(new Architecture(OS.Windows, Cpu.I486)).Should().BeTrue();
            new Architecture(OS.Linux, Cpu.I586).IsCompatible(new Architecture(OS.Linux, Cpu.I586)).Should().BeTrue();
            new Architecture(OS.MacOSX, Cpu.I686).IsCompatible(new Architecture(OS.MacOSX, Cpu.I686)).Should().BeTrue();

            new Architecture(OS.Windows, Cpu.I486).IsCompatible(new Architecture(OS.Linux, Cpu.I486)).Should().BeFalse();
            new Architecture(OS.Windows, Cpu.I486).IsCompatible(new Architecture(OS.Linux, Cpu.Ppc)).Should().BeFalse();
        }

        [Fact]
        public void TestIsCompatibleOS()
        {
            // Wildcard
            OS.All.IsCompatible(OS.Windows).Should().BeTrue();
            OS.All.IsCompatible(OS.Linux).Should().BeTrue();
            OS.All.IsCompatible(OS.MacOSX).Should().BeTrue();

            // Mismatch
            OS.Windows.IsCompatible(OS.Linux).Should().BeFalse();
            OS.Linux.IsCompatible(OS.Windows).Should().BeFalse();
            OS.MacOSX.IsCompatible(OS.Linux).Should().BeFalse();
            OS.Linux.IsCompatible(OS.MacOSX).Should().BeFalse();

            // Superset
            OS.Windows.IsCompatible(OS.Cygwin).Should().BeTrue();
            OS.Cygwin.IsCompatible(OS.Windows).Should().BeFalse();
            OS.Darwin.IsCompatible(OS.MacOSX).Should().BeTrue();
            OS.MacOSX.IsCompatible(OS.Darwin).Should().BeFalse();
            OS.Posix.IsCompatible(OS.Linux).Should().BeTrue();
            OS.Posix.IsCompatible(OS.Solaris).Should().BeTrue();
            OS.Posix.IsCompatible(OS.FreeBsd).Should().BeTrue();
            OS.Posix.IsCompatible(OS.Darwin).Should().BeTrue();
            OS.Posix.IsCompatible(OS.MacOSX).Should().BeTrue();
            OS.Posix.IsCompatible(OS.Posix).Should().BeTrue();
            OS.Posix.IsCompatible(OS.Windows).Should().BeFalse();
        }

        [Fact]
        public void TestIsCompatibleCpu()
        {
            // Wildcard
            Cpu.All.IsCompatible(Cpu.I486).Should().BeTrue();
            Cpu.All.IsCompatible(Cpu.I586).Should().BeTrue();
            Cpu.All.IsCompatible(Cpu.I686).Should().BeTrue();

            // Mismatch
            Cpu.I686.IsCompatible(Cpu.Ppc).Should().BeFalse();
            Cpu.Ppc.IsCompatible(Cpu.I586).Should().BeFalse();

            // x86-series backwards-compatibility
            Cpu.I386.IsCompatible(Cpu.I686).Should().BeTrue();
            Cpu.I686.IsCompatible(Cpu.I386).Should().BeFalse();

            // 32bit/64bit exclusion
            Cpu.I386.IsCompatible(Cpu.X64).Should().BeFalse();
            Cpu.X64.IsCompatible(Cpu.I686).Should().BeFalse();
            Cpu.Ppc.IsCompatible(Cpu.Ppc64).Should().BeFalse();
            Cpu.Ppc64.IsCompatible(Cpu.Ppc).Should().BeFalse();
        }
    }
}
