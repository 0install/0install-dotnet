// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using NanoByte.Common.Values;
using ZeroInstall.Model.Design;
using static System.Runtime.InteropServices.Architecture;
using static System.Runtime.InteropServices.RuntimeInformation;

namespace ZeroInstall.Model;

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

        OS = ToOS(architectureArray[0]);
        Cpu = ToCpu(architectureArray[1]);
    }

    /// <summary>
    /// An architecture representing the currently running system.
    /// </summary>
    public static readonly Architecture CurrentSystem = new(CurrentOS, CurrentCpu);

    private static OS CurrentOS
    {
        get
        {
            if (WindowsUtils.IsWindows) return OS.Windows;
            if (UnixUtils.IsMacOSX) return OS.MacOSX;
            if (UnixUtils.IsUnix) return ToOS(UnixUtils.OSName);
            return OS.Unknown;
        }
    }

    private static Cpu CurrentCpu
    {
        get
        {
            if (UnixUtils.IsUnix) return ToCpu(UnixUtils.CpuType);
            return OSArchitecture switch
            {
                X86 => Cpu.I686,
                X64 => Cpu.X64,
                Arm => Cpu.ArmV7L,
                Arm64 => Cpu.AArch64,
                _ => Cpu.Unknown
            };
        }
    }

    private static OS ToOS(string value) => value switch
    {
        "*" or "all" or "any" or "(none)" => OS.All,
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

    private static Cpu ToCpu(string value) => value switch
    {
        "*" or "all" or "any" or "noarch" or "(none)" => Cpu.All,
        "i386" => Cpu.I386,
        "x86" => Cpu.I386,
        "i486" => Cpu.I486,
        "i586" => Cpu.I586,
        "i686" => Cpu.I686,
        "i86pc" => Cpu.I686,
        "x86_64" => Cpu.X64,
        "amd64" => Cpu.X64,
        "ppc" => Cpu.Ppc,
        "ppc32" => Cpu.Ppc,
        "Power Macintosh" => Cpu.Ppc,
        "ppc64" => Cpu.Ppc64,
        "armv6l" or "armv6h" or "armhf" => Cpu.ArmV6L,
        "armv7l" => Cpu.ArmV7L,
        "aarch64" or "arm64" => Cpu.AArch64,
        "src" => Cpu.Source,
        _ => Cpu.Unknown
    };

    /// <summary>
    /// Returns the architecture in the form "os-cpu". Safe for parsing!
    /// </summary>
    public override string ToString()
        => OS.ConvertToString() + "-" + Cpu.ConvertToString();

    /// <inheritdoc/>
    public bool Equals(Architecture other)
        => other.OS == OS && other.Cpu == Cpu;

    public static bool operator ==(Architecture left, Architecture right) => left.Equals(right);
    public static bool operator !=(Architecture left, Architecture right) => !left.Equals(right);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is Architecture architecture && Equals(architecture);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(OS, Cpu);
}
