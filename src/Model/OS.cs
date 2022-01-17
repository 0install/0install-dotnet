// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Values.Design;

namespace ZeroInstall.Model;

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
