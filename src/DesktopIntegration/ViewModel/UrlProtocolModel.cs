// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Linq;
using NanoByte.Common;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration.ViewModel;

/// <summary>
/// Wraps a <see cref="UrlProtocol"/> for data binding.
/// </summary>
public class UrlProtocolModel : IconCapabilityModel
{
    private readonly UrlProtocol _urlProtocol;

    /// <summary>
    /// All <see cref="UrlProtocol.KnownPrefixes"/> concatenated with ", ". If no <see cref="UrlProtocol.KnownPrefixes"/> is available <see cref="Capability.ID"/> will be returned.
    /// </summary>
    public string KnownPrefixes => _urlProtocol.KnownPrefixes.Count == 0 ? Capability.ID : StringUtils.Join(", ", _urlProtocol.KnownPrefixes.Select(extension => extension.Value));

    /// <inheritdoc/>
    public UrlProtocolModel(UrlProtocol capability, bool used)
        : base(capability, used)
    {
        _urlProtocol = capability;
    }
}
