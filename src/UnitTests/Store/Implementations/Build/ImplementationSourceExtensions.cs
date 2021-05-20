// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.Linq;

namespace ZeroInstall.Store.Implementations.Build
{
    public static class ImplementationSourceExtensions
    {
        /// <summary>
        /// Compares with a set of <see cref="ArchiveImplementationSource"/>, ignoring <see cref="ArchiveImplementationSource.Path"/> to allow easier testing with randomized paths.
        /// </summary>
        public static bool IsEquivalentTo(this IEnumerable<IImplementationSource> sources, IEnumerable<ArchiveImplementationSource> archiveSources)
            => sources.OfType<ArchiveImplementationSource>()
                      .Select(x => x with {Path = "dummy", OriginalSource = "dummy"})
                      .SequenceEqual(archiveSources.Select(x => x with {Path = "dummy", OriginalSource = "dummy"}));
    }
}
