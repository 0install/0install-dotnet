// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;

namespace ZeroInstall.Model;

/// <summary>
/// Contains test methods for <see cref="Architecture"/>.
/// </summary>
public class ArchitectureTest
{
    [Fact]
    public void Constructor()
    {
        new Architecture("*-*").Should().Be(new Architecture(OS.All, Cpu.All));
        new Architecture("Linux-*").Should().Be(new Architecture(OS.Linux, Cpu.All));
        new Architecture("*-i686").Should().Be(new Architecture(OS.All, Cpu.I686));
        new Architecture("Linux-i686").Should().Be(new Architecture(OS.Linux, Cpu.I686));
    }

    [Fact]
    public void RunsOn()
    {
        new Architecture(OS.Windows, Cpu.I486).RunsOn(new Architecture(OS.Windows, Cpu.I486)).Should().BeTrue();
        new Architecture(OS.Linux, Cpu.I586).RunsOn(new Architecture(OS.Linux, Cpu.I586)).Should().BeTrue();
        new Architecture(OS.MacOSX, Cpu.I686).RunsOn(new Architecture(OS.MacOSX, Cpu.I686)).Should().BeTrue();

        new Architecture(OS.Windows, Cpu.I486).RunsOn(new Architecture(OS.Linux, Cpu.I486)).Should().BeFalse();
        new Architecture(OS.Windows, Cpu.I486).RunsOn(new Architecture(OS.Linux, Cpu.Ppc)).Should().BeFalse();

        // Windows/macOS ARM x86/x64 emulation
        new Architecture(OS.Windows, Cpu.I486).RunsOn(new Architecture(OS.Windows, Cpu.AArch64)).Should().BeTrue();
        new Architecture(OS.Windows, Cpu.X64).RunsOn(new Architecture(OS.Windows, Cpu.AArch64)).Should().BeTrue();
        new Architecture(OS.Windows, Cpu.I486).RunsOn(new Architecture(OS.MacOSX, Cpu.AArch64)).Should().BeTrue();
        new Architecture(OS.Windows, Cpu.X64).RunsOn(new Architecture(OS.MacOSX, Cpu.AArch64)).Should().BeTrue();
    }

    [Fact]
    public void RunsOnOS()
    {
        // Wildcard
        OS.All.RunsOn(OS.Windows).Should().BeTrue();
        OS.All.RunsOn(OS.Linux).Should().BeTrue();
        OS.All.RunsOn(OS.MacOSX).Should().BeTrue();

        // Mismatch
        OS.Windows.RunsOn(OS.Linux).Should().BeFalse();
        OS.Linux.RunsOn(OS.Windows).Should().BeFalse();
        OS.MacOSX.RunsOn(OS.Linux).Should().BeFalse();
        OS.Linux.RunsOn(OS.MacOSX).Should().BeFalse();

        // Superset
        OS.Windows.RunsOn(OS.Cygwin).Should().BeTrue();
        OS.Cygwin.RunsOn(OS.Windows).Should().BeFalse();
        OS.Darwin.RunsOn(OS.MacOSX).Should().BeTrue();
        OS.MacOSX.RunsOn(OS.Darwin).Should().BeFalse();
        OS.Posix.RunsOn(OS.Linux).Should().BeTrue();
        OS.Posix.RunsOn(OS.Solaris).Should().BeTrue();
        OS.Posix.RunsOn(OS.FreeBsd).Should().BeTrue();
        OS.Posix.RunsOn(OS.Darwin).Should().BeTrue();
        OS.Posix.RunsOn(OS.MacOSX).Should().BeTrue();
        OS.Posix.RunsOn(OS.Posix).Should().BeTrue();
        OS.Posix.RunsOn(OS.Windows).Should().BeFalse();
    }

    [Fact]
    public void RunsOnCpu()
    {
        // Wildcard
        Cpu.All.RunsOn(Cpu.I486).Should().BeTrue();
        Cpu.All.RunsOn(Cpu.I586).Should().BeTrue();
        Cpu.All.RunsOn(Cpu.I686).Should().BeTrue();

        // Mismatch
        Cpu.I686.RunsOn(Cpu.Ppc).Should().BeFalse();
        Cpu.Ppc.RunsOn(Cpu.I586).Should().BeFalse();

        // x86-series backwards-compatibility
        Cpu.I386.RunsOn(Cpu.I686).Should().BeTrue();
        Cpu.I686.RunsOn(Cpu.I386).Should().BeFalse();

        // 32bit/64bit compatibility
        Cpu.I386.RunsOn(Cpu.X64).Should().BeTrue();
        Cpu.X64.RunsOn(Cpu.I686).Should().BeFalse();
        Cpu.Ppc.RunsOn(Cpu.Ppc64).Should().BeTrue();
        Cpu.Ppc64.RunsOn(Cpu.Ppc).Should().BeFalse();
    }
}
