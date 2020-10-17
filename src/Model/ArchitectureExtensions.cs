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
        /// Determines whether an <paramref name="implementation"/> architecture (the current instance) can run on a <paramref name="system"/> architecture.
        /// </summary>
        public static bool IsCompatible(this Architecture implementation, Architecture system)
        {
            if (implementation.OS.IsCompatible(system.OS) && implementation.Cpu.IsCompatible(system.Cpu)) return true;

            // Windows on ARM x86 emulation
            if (system.OS == OS.Windows && system.Cpu == Cpu.AArch64 && implementation.Cpu >= Cpu.I386 && implementation.Cpu <= Cpu.I686) return true;

            return false;
        }

        /// <summary>
        /// Determines whether an <paramref name="implementation"/> OS is compatible with a <paramref name="system"/> OS.
        /// </summary>
        public static bool IsCompatible(this OS implementation, OS system)
        {
            if (implementation == OS.Unknown || system == OS.Unknown) return false;

            // Exact OS match or platform-neutral implementation
            if (implementation == system || implementation == OS.All || system == OS.All) return true;

            // Compatible supersets
            if (implementation == OS.Windows && system == OS.Cygwin) return true;
            if (implementation == OS.Darwin && system == OS.MacOSX) return true;
            if (implementation == OS.Posix && system < OS.Windows) return true;

            // No match
            return false;
        }

        /// <summary>
        /// Determines whether an <paramref name="implementation"/> CPU is compatible with a <paramref name="system"/> CPU.
        /// </summary>
        public static bool IsCompatible(this Cpu implementation, Cpu system)
        {
            if (implementation == Cpu.Unknown || system == Cpu.Unknown) return false;

            // Exact CPU match or platform-neutral implementation
            if (implementation == system || implementation == Cpu.All || system == Cpu.All) return true;

            // Compatible supersets
            if (implementation >= Cpu.I386 && implementation <= Cpu.X64 && system >= implementation && system <= Cpu.X64) return true;
            if (implementation >= Cpu.Ppc && implementation <= Cpu.Ppc64 && system >= implementation && system <= Cpu.Ppc64) return true;
            if (implementation >= Cpu.ArmV6L && implementation <= Cpu.AArch64 && system >= implementation && system <= Cpu.AArch64) return true;

            // No match
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
