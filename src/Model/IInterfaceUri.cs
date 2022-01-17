// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// An object that contains an interface URI.
/// </summary>
public interface IInterfaceUri
{
    /// <summary>
    /// An interface URI (URL or file path).
    /// </summary>
    FeedUri InterfaceUri { get; set; }
}
