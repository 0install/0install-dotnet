// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Configuration;

/// <summary>
/// Controls how liberally network access is attempted.
/// </summary>
/// <see cref="Config.NetworkUse"/>
public enum NetworkLevel
{
    /// <summary>Do not access network at all.</summary>
    Offline,

    /// <summary>Only access network when there are no safe implementations available.</summary>
    Minimal,

    /// <summary>Always use network to get the newest available versions.</summary>
    Full
}
