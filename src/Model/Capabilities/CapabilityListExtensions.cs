// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// Contains extension methods for <see cref="CapabilityList"/>s.
/// </summary>
public static class CapabilityListExtensions
{
    extension(IEnumerable<CapabilityList> capabilityLists)
    {
        /// <summary>
        /// Flattens a set of <see cref="CapabilityList"/>s into a single stream of <see cref="Capability"/>s, filtering out <see cref="CapabilityList.OS"/>es that do not match <see cref="Architecture.CurrentSystem"/>.
        /// </summary>
        [LinqTunnel]
        public IEnumerable<Capability> CompatibleCapabilities()
            => capabilityLists.Where(x => x.OS.RunsOn(Architecture.CurrentSystem.OS))
                              .SelectMany(x => x.Entries);
    }
}
