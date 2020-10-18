// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model
{
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

            // Windows on ARM x86 emulation
            if (target.OS == OS.Windows && target.Cpu == Cpu.AArch64 && architecture.Cpu >= Cpu.I386 && architecture.Cpu <= Cpu.I686) return true;

            return false;
        }

        /// <summary>
        /// Determines whether an implementation for <paramref name="os"/> can run on <paramref name="target"/>.
        /// </summary>
        public static bool RunsOn(this OS os, OS target)
        {
            if (os == OS.Unknown || target == OS.Unknown) return false;

            // Exact OS match or platform-neutral implementation
            if (os == target || os == OS.All || target == OS.All) return true;

            // Compatible supersets
            if (os == OS.Windows && target == OS.Cygwin) return true;
            if (os == OS.Darwin && target == OS.MacOSX) return true;
            if (os == OS.Posix && target <= OS.Cygwin) return true;

            return false;
        }

        /// <summary>
        /// Determines whether an implementation for <paramref name="cpu"/> can run on <paramref name="target"/>.
        /// </summary>
        public static bool RunsOn(this Cpu cpu, Cpu target)
        {
            if (cpu == Cpu.Unknown || target == Cpu.Unknown) return false;

            // Exact CPU match or platform-neutral implementation
            if (cpu == target || cpu == Cpu.All || target == Cpu.All) return true;

            // Compatible supersets
            if (cpu >= Cpu.I386 && cpu <= Cpu.X64 && target >= cpu && target <= Cpu.X64) return true;
            if (cpu >= Cpu.Ppc && cpu <= Cpu.Ppc64 && target >= cpu && target <= Cpu.Ppc64) return true;
            if (cpu >= Cpu.ArmV6L && cpu <= Cpu.AArch64 && target >= cpu && target <= Cpu.AArch64) return true;

            return false;
        }

        /// <summary>
        /// Indicates whether the CPU architecture is 32-bit.
        /// </summary>
        public static bool Is32Bit(this Cpu cpu)
            => cpu >= Cpu.I386 && cpu <= Cpu.I686
            || cpu == Cpu.Ppc
            || cpu == Cpu.ArmV6L
            || cpu == Cpu.ArmV7L;

        /// <summary>
        /// Indicates whether the CPU architecture is 64-bit.
        /// </summary>
        public static bool Is64Bit(this Cpu cpu)
            => cpu == Cpu.X64
            || cpu == Cpu.Ppc64
            || cpu == Cpu.AArch64;
    }
}
