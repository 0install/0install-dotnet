// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Model.Capabilities;
using AutoPlay = ZeroInstall.DesktopIntegration.AccessPoints.AutoPlay;
using ContextMenu = ZeroInstall.DesktopIntegration.AccessPoints.ContextMenu;
using DefaultProgram = ZeroInstall.DesktopIntegration.AccessPoints.DefaultProgram;
using FileType = ZeroInstall.DesktopIntegration.AccessPoints.FileType;
using UrlProtocol = ZeroInstall.DesktopIntegration.AccessPoints.UrlProtocol;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Contains extension methods for <see cref="Capability"/>s.
/// </summary>
public static class CapabilityExtensions
{
    /// <summary>
    /// Creates a <see cref="DefaultAccessPoint"/> referencing a specific <see cref="DefaultCapability"/>.
    /// </summary>
    /// <param name="capability">The <see cref="DefaultCapability"/> to create a <see cref="DefaultAccessPoint"/> for.</param>
    /// <returns>The newly created <see cref="DefaultAccessPoint"/>.</returns>
    public static AccessPoint ToAccessPoint(this DefaultCapability capability)
        => (capability ?? throw new ArgumentNullException(nameof(capability))) switch
        {
            Model.Capabilities.AutoPlay x => new AutoPlay {Capability = x.ID},
            Model.Capabilities.ContextMenu x => new ContextMenu {Capability = x.ID},
            Model.Capabilities.DefaultProgram x => new DefaultProgram {Capability = x.ID},
            Model.Capabilities.FileType x => new FileType {Capability = x.ID},
            Model.Capabilities.UrlProtocol x => new UrlProtocol {Capability = x.ID},
            _ => throw new NotSupportedException($"Unknown capability: {capability}")
        };
}