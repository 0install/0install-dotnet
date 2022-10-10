// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Selection;

/// <summary>
/// Provides extension methods for <see cref="SelectionCandidate"/>.
/// </summary>
public static class SelectionCandidateExtensions
{
    /// <summary>
    /// Returns a deduplicated list of suitable version numbers, sorted from newest to oldest.
    /// </summary>
    public static IEnumerable<ImplementationVersion> GetSuitableVersions(this IEnumerable<SelectionCandidate> candidates)
        => candidates.Where(x => x.IsSuitable)
                     .Select(x => x.Version)
                     .WhereNotNull()
                     .Distinct()
                     .OrderByDescending(x => x);
}
