// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Client;

/// <summary>
/// Zero Install is temporarily unavailable. Try again in a few seconds.
/// </summary>
public class TemporarilyUnavailableException : Exception
{
    public TemporarilyUnavailableException()
        : base("Zero Install is temporarily unavailable. Try again in a few seconds.")
    {}
}
