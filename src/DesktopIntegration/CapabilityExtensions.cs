// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using JetBrains.Annotations;
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
        public static AccessPoint ToAccessPoint([NotNull] this Store.Model.Capabilities.DefaultCapability capability)
        {
            switch (capability ?? throw new ArgumentNullException(nameof(capability)))
            {
                case Store.Model.Capabilities.AutoPlay x: return new AutoPlay {Capability = x.ID};
                case Store.Model.Capabilities.ContextMenu x: return new ContextMenu {Capability = x.ID};
                case Store.Model.Capabilities.DefaultProgram x: return new DefaultProgram {Capability = x.ID};
                case Store.Model.Capabilities.FileType x: return new FileType {Capability = x.ID};
                case Store.Model.Capabilities.UrlProtocol x: return new UrlProtocol {Capability = x.ID};
                default: throw new NotSupportedException($"Unknown capability: {capability}");
            }
        }
    }
}
