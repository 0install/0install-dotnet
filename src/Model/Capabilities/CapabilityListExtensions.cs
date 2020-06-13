// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.Linq;

namespace ZeroInstall.Model.Capabilities
{
    /// <summary>
    /// Contains extension methods for <see cref="CapabilityList"/>s.
    /// </summary>
    public static class CapabilityListExtensions
    {
        /// <summary>
        /// Flattens a set of <see cref="CapabilityList"/>s into a single stream of <see cref="Capability"/>s, filtering out <see cref="CapabilityList.OS"/>es that do not match <see cref="Architecture.CurrentSystem"/>.
        /// </summary>
        public static IEnumerable<Capability> CompatibleCapabilities(this IEnumerable<CapabilityList> capabilityLists)
            => capabilityLists.Where(x => x.OS.IsCompatible(Architecture.CurrentSystem.OS))
                              .SelectMany(x => x.Entries);
    }
}
