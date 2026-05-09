// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Values.Design;

namespace ZeroInstall.Model;

/// <summary>
/// Describes a CPU architecture.
/// </summary>
[TypeConverter(typeof(EnumXmlConverter<Cpu>))]
public enum Cpu
{
    /// <summary>Supports all CPU architectures (e.g. developed with cross-platform language like Java).</summary>
    [XmlEnum("*")]
    All,

    /// <summary>Supports CPUs with the i386 architecture or newer.</summary>
    [XmlEnum("i386")]
    I386,

    /// <summary>Supports CPUs with the i486 architecture or newer.</summary>
    [XmlEnum("i486")]
    I486,

    /// <summary>Supports CPUs with the i586 architecture or newer.</summary>
    [XmlEnum("i586")]
    I586,

    /// <summary>Supports CPUs with the i686.</summary>
    [XmlEnum("i686")]
    I686,

    /// <summary>Requires an x86-64 capable CPU.</summary>
    [XmlEnum("x86_64")]
    X64,

    /// <summary>Supports CPUs with the PowerPC-architecture.</summary>
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

    /// <summary>Supports ARMv8 CPUs in 64-bit mode.</summary>
    [XmlEnum("aarch64")]
    AArch64,

    /// <summary>Supports ARM64 CPUs but uses x64-compatible calling convention (Windows-specific).</summary>
    [XmlEnum("arm64ec")]
    Arm64EC,

    /// <summary>This is a source release and therefore architecture-independent.</summary>
    [XmlEnum("src")]
    Source = 99,

    /// <summary>The supported CPU architecture has not been set yet.</summary>
    [XmlEnum("unknown")]
    Unknown = 100
}
