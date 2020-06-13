// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common.Native;
using NanoByte.Common.Values;
using NanoByte.Common.Values.Design;
using ZeroInstall.Model.Design;
using ZeroInstall.Model.Properties;

#if NETSTANDARD
using System.Runtime.InteropServices;
#endif

namespace ZeroInstall.Model
{
    #region Enumerations
    /// <summary>
    /// Describes an operating system family.
    /// </summary>
    [TypeConverter(typeof(EnumXmlConverter<OS>))]
    public enum OS
    {
        /// <summary>Supports all operating systems (e.g. developed with cross-platform language like Java).</summary>
        [XmlEnum("*")]
        All,

        /// <summary>Everything except <see cref="Windows"/>.</summary>
        [XmlEnum("POSIX")]
        Posix,

        /// <summary>Supports only Linux operating systems.</summary>
        [XmlEnum("Linux")]
        Linux,

        /// <summary>Supports only Solaris.</summary>
        [XmlEnum("Solaris")]
        Solaris,

        /// <summary>Supports only FreeBSD.</summary>
        [XmlEnum("FreeBSD")]
        FreeBsd,

        /// <summary>MacOSX, without the proprietary bits.</summary>
        [XmlEnum("Darwin")]
        Darwin,

        /// <summary>Supports only MacOS X.</summary>
        [XmlEnum("MacOSX")]
        MacOSX,

        /// <summary>A Unix-compatibility layer for Windows.</summary>
        [XmlEnum("Cygwin")]
        Cygwin,

        /// <summary>Supports only Windows NT 5.0+ (Windows 2000, XP, 2003, Vista, 2008, 7, 2008 R2, ...).</summary>
        [XmlEnum("Windows")]
        Windows,

        /// <summary>The supported operating system has not been set yet.</summary>
        [XmlEnum("unknown")]
        Unknown = 100
    }

    /// <summary>
    /// Describes a CPU architecture.
    /// </summary>
    [TypeConverter(typeof(EnumXmlConverter<Cpu>))]
    public enum Cpu
    {
        /// <summary>Supports all CPU architectures (e.g. developed with cross-platform language like Java).</summary>
        [XmlEnum("*")]
        All,

        /// <summary>Supports CPUs with the i386 architecture or newer (up to i686).</summary>
        [XmlEnum("i386")]
        I386,

        /// <summary>Supports CPUs with the i486 architecture or newer (up to i686).</summary>
        [XmlEnum("i486")]
        I486,

        /// <summary>Supports CPUs with the i586 architecture or newer (up to i686).</summary>
        [XmlEnum("i586")]
        I586,

        /// <summary>Supports CPUs with the i686.</summary>
        [XmlEnum("i686")]
        I686,

        /// <summary>Requires a x86-64 capable CPU.</summary>
        [XmlEnum("x86_64")]
        X64,

        /// <summary>Supports CPUs with the PowerPC-architecture (used in older Macs).</summary>
        [XmlEnum("ppc")]
        Ppc,

        /// <summary>Requires a 64-bit capable PowerPC CPU.</summary>
        [XmlEnum("ppc64")]
        Ppc64,

        /// <summary>Supports ARMv6 CPUs in little-endian mode.</summary>
        [XmlEnum("armv6l")]
        ArmV6L,

        /// <summary>Supports ARMv7 CPUs in little-endian mode.</summary>
        [XmlEnum("armv7l")]
        ArmV7L,

        /// <summary>This is a source release and therefore architecture-independent.</summary>
        [XmlEnum("src")]
        Source = 99,

        /// <summary>The supported CPU architecture has not been set yet.</summary>
        [XmlEnum("unknown")]
        Unknown = 100
    }
    #endregion

    /// <summary>
    /// Describes a combination of an operating system and a CPU architecture.
    /// </summary>
    [Description("Describes a combination of an operating system and a CPU architecture.")]
    [TypeConverter(typeof(ArchitectureConverter))]
    [Serializable]
    public struct Architecture : IEquatable<Architecture>
    {
        /// <summary>
        /// Determines which operating systems are supported.
        /// </summary>
        [Description("Determines which operating systems are supported.")]
        public OS OS { get; set; }

        /// <summary>
        /// Determines which CPU-architectures are supported.
        /// </summary>
        [Description("Determines which CPU-architectures are supported.")]
        public Cpu Cpu { get; set; }

        /// <summary>
        /// An architecture representing the currently running system.
        /// </summary>
        public static readonly Architecture CurrentSystem = GetCurrentSystem();

        private static Architecture GetCurrentSystem()
        {
            OS GetOS()
            {
                if (WindowsUtils.IsWindows) return OS.Windows;
                if (UnixUtils.IsMacOSX) return OS.MacOSX;
                if (UnixUtils.IsUnix) return ParseOSString(UnixUtils.OSName);
                return OS.Unknown;
            }

            Cpu GetCpu()
            {
#if NETSTANDARD
                return RuntimeInformation.OSArchitecture switch
                {
                    System.Runtime.InteropServices.Architecture.Arm => Cpu.ArmV7L,
                    System.Runtime.InteropServices.Architecture.Arm64 => Cpu.ArmV7L,
                    System.Runtime.InteropServices.Architecture.X86 => Cpu.I686,
                    System.Runtime.InteropServices.Architecture.X64 => Cpu.X64,
                    _ => Cpu.Unknown
                };
#else
                if (WindowsUtils.IsWindows) return OSUtils.Is64BitOperatingSystem ? Cpu.X64 : Cpu.I686;
                else if (UnixUtils.IsUnix) return ParseCpuString(UnixUtils.CpuType);
                else return Cpu.Unknown;
#endif
            }

            return new Architecture(GetOS(), GetCpu());
        }

        /// <summary>
        /// Creates a new architecture structure from a string in the form "os-cpu".
        /// </summary>
        /// <exception cref="FormatException"><paramref name="architecture"/> is not in the form "os-cpu"</exception>
        public Architecture(string architecture)
            : this()
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(architecture)) throw new ArgumentNullException(nameof(architecture));
            #endregion

            var architectureArray = architecture.Split('-');
            if (architectureArray.Length != 2) throw new FormatException(Resources.ArchitectureStringFormat);

            OS = ParseOSString(architectureArray[0]);
            Cpu = ParseCpuString(architectureArray[1]);
        }

        /// <summary>
        /// Creates a new architecture structure with pre-set values.
        /// </summary>
        /// <param name="os">Determines which operating systems are supported.</param>
        /// <param name="cpu">Determines which CPU-architectures are supported.</param>
        public Architecture(OS os, Cpu cpu)
            : this()
        {
            OS = os;
            Cpu = cpu;
        }

        #region Parse string
        private static OS ParseOSString(string os)
            //try { return os.ConvertFromString<OS>(); }
            //catch (ArgumentException) { return OS.Unknown; }
            // NOTE: Use hard-coded switch instead of reflection-based code for better performance
            => os switch
            {
                "*" => OS.All,
                "Linux" => OS.Linux,
                "Solaris" => OS.Solaris,
                "FreeBSD" => OS.FreeBsd,
                "MacOSX" => OS.MacOSX,
                "Darwin" => OS.Darwin,
                "Cygwin" => OS.Cygwin,
                "POSIX" => OS.Posix,
                "Windows" => OS.Windows,
                _ => OS.Unknown
            };

        private static Cpu ParseCpuString(string cpu)
        {
            //try { return cpu.ConvertFromString<Cpu>(); }
            //catch (ArgumentException) { return Cpu.Unknown; }

            // NOTE: Use hard-coded switch instead of reflection-based code for better performance
            return cpu switch
            {
                "*" => Cpu.All,
                "i386" => Cpu.I386,
                "i486" => Cpu.I486,
                "i586" => Cpu.I586,
                "i686" => Cpu.I686,
                "x86_64" => Cpu.X64,
                "ppc" => Cpu.Ppc,
                "ppc64" => Cpu.Ppc64,
                "src" => Cpu.Source,
                _ => Cpu.Unknown
            };
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the architecture in the form "os-cpu". Safe for parsing!
        /// </summary>
        public override string ToString() => OS.ConvertToString() + "-" + Cpu.ConvertToString();
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Architecture other)
            => other.OS == OS && other.Cpu == Cpu;

        public static bool operator ==(Architecture left, Architecture right) => left.Equals(right);
        public static bool operator !=(Architecture left, Architecture right) => !left.Equals(right);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj != null && obj is Architecture architecture && Equals(architecture);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(OS, Cpu);
        #endregion
    }
}
