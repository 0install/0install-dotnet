// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Well-known OS distributions.
/// </summary>
public static class KnownDistributions
{
    public const string
        Arch = "Arch",
        Cygwin = "Cygwin",
        Darwin = "Darwin",
        Debian = "Debian",
        Gentoo = "Gentoo",
        MacPorts = "MacPorts",
        Ports = "Ports",
        Rpm = "RPM",
        Slack = "Slack",
        Windows = "Windows",
        WinGet = "WinGet";

    public static readonly string[] All = [Arch, Cygwin, Darwin, Debian, Gentoo, MacPorts, Ports, Rpm, Slack, Windows, WinGet];
}
