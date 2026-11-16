// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Selection;

/// <summary>
/// Provides extension methods for <see cref="SelectionCandidate"/>.
/// </summary>
public static class SelectionCandidateExtensions
{
    extension(IEnumerable<SelectionCandidate> candidates)
    {
        /// <summary>
        /// Returns a deduplicated list of suitable version numbers, sorted from newest to oldest.
        /// </summary>
        public IEnumerable<ImplementationVersion> GetSuitableVersions()
            => candidates.Where(x => x.IsSuitable)
                         .Select(x => x.Version)
                         .WhereNotNull()
                         .Distinct()
                         .OrderByDescending(x => x);
    }
}
