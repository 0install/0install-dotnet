// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using ZeroInstall.DesktopIntegration.AccessPoints;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Contains extension methods for <see cref="Store.Model.Capabilities.Capability"/>s.
    /// </summary>
    public static class CapabilityExtensions
    {
        /// <summary>
        /// Creates a <see cref="DefaultAccessPoint"/> referencing a specific <see cref="Store.Model.Capabilities.DefaultCapability"/>.
        /// </summary>
        /// <param name="capability">The <see cref="Store.Model.Capabilities.DefaultCapability"/> to create a <see cref="DefaultAccessPoint"/> for.</param>
        /// <returns>The newly created <see cref="DefaultAccessPoint"/>.</returns>
        public static AccessPoint ToAccessPoint(this Store.Model.Capabilities.DefaultCapability capability)
            => (capability ?? throw new ArgumentNullException(nameof(capability))) switch
            {
                Store.Model.Capabilities.AutoPlay x => (AccessPoint)new AutoPlay {Capability = x.ID},
                Store.Model.Capabilities.ContextMenu x => new ContextMenu {Capability = x.ID},
                Store.Model.Capabilities.DefaultProgram x => new DefaultProgram {Capability = x.ID},
                Store.Model.Capabilities.FileType x => new FileType {Capability = x.ID},
                Store.Model.Capabilities.UrlProtocol x => new UrlProtocol {Capability = x.ID},
                _ => throw new NotSupportedException($"Unknown capability: {capability}")
            };
    }
}
