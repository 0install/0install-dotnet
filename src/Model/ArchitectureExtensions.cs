// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Contains extension methods for <see cref="Architecture"/>, <see cref="OS"/> and <see cref="Cpu"/>.
/// </summary>
public static class ArchitectureExtensions
{
    /// <summary>
    /// Determines whether an implementation for <paramref name="architecture"/> can run on <paramref name="target"/>.
    /// </summary>
    public static bool RunsOn(this Architecture architecture, Architecture target)
    {
        if (architecture.OS.RunsOn(target.OS) && architecture.Cpu.RunsOn(target.Cpu)) return true;

        // Windows/macOS ARM x86/x64 emulation
        if (target.OS is OS.Windows or OS.MacOSX && target.Cpu == Cpu.AArch64 && architecture.Cpu is (>= Cpu.I386 and <= Cpu.X64)) return true;

        return false;
    }

    /// <summary>
    /// Determines whether an implementation for <paramref name="os"/> can run on <paramref name="target"/>.
    /// </summary>
    public static bool RunsOn(this OS os, OS target)
        => os switch
        {
            OS.Unknown or OS.Unknown => false,

            // Exact match
            _ when os == target => true,

            // Platform-neutral implementation
            OS.All => true,
            _ when target == OS.All => true,

            // Compatible supersets
            OS.Windows when target == OS.Cygwin => true,
            OS.Darwin when target == OS.MacOSX => true,
            OS.Posix when target <= OS.Cygwin => true,

            _ => false
        };

    /// <summary>
    /// Determines whether an implementation for <paramref name="cpu"/> can run on <paramref name="target"/>.
    /// </summary>
    public static bool RunsOn(this Cpu cpu, Cpu target)
        => cpu switch
        {
            Cpu.Unknown or Cpu.Unknown => false,

            // Exact match
            _ when cpu == target => true,

            // Platform-neutral implementation
            Cpu.All => true,
            _ when target == Cpu.All => true,

            // Compatible supersets
            >= Cpu.I386 and <= Cpu.X64 when target >= cpu && target <= Cpu.X64 => true,
            >= Cpu.Ppc and <= Cpu.Ppc64 when target >= cpu && target <= Cpu.Ppc64 => true,
            >= Cpu.ArmV6L and <= Cpu.AArch64 when target >= cpu && target <= Cpu.AArch64 => true,

            _ => false
        };

    /// <summary>
    /// Indicates whether the CPU architecture is 32-bit.
    /// </summary>
    public static bool Is32Bit(this Cpu cpu)
        => cpu is ((>= Cpu.I386 and <= Cpu.I686) or Cpu.Ppc or Cpu.ArmV6L or Cpu.ArmV7L);

    /// <summary>
    /// Indicates whether the CPU architecture is 64-bit.
    /// </summary>
    public static bool Is64Bit(this Cpu cpu)
        => cpu is (Cpu.X64 or Cpu.Ppc64 or Cpu.AArch64);
}
