// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License


namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Contains extension methods for <see cref="Capability"/>s.
/// </summary>
public static class CapabilityExtensions
{
    /// <summary>
    /// Creates a <see cref="AccessPoints.DefaultAccessPoint"/> referencing a specific <see cref="DefaultCapability"/>.
    /// </summary>
    /// <param name="capability">The <see cref="DefaultCapability"/> to create a <see cref="AccessPoints.DefaultAccessPoint"/> for.</param>
    /// <returns>The newly created <see cref="AccessPoints.DefaultAccessPoint"/>.</returns>
    public static AccessPoints.AccessPoint ToAccessPoint(this DefaultCapability capability)
        => (capability ?? throw new ArgumentNullException(nameof(capability))) switch
        {
            AutoPlay x => new AccessPoints.AutoPlay {Capability = x.ID},
            ContextMenu x => new AccessPoints.ContextMenu {Capability = x.ID},
            DefaultProgram x => new AccessPoints.DefaultProgram {Capability = x.ID},
            FileType x => new AccessPoints.FileType {Capability = x.ID},
            UrlProtocol x => new AccessPoints.UrlProtocol {Capability = x.ID},
            _ => throw new NotSupportedException($"Unknown capability: {capability}")
        };
}
