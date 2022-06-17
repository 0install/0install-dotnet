// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Generates <see cref="SelectionCandidate"/>s for <see cref="ISolver"/>s to choose among.
/// </summary>
/// <remarks>Implementations of this interface may perform in-memory caching and are thread-safe.</remarks>
public interface ISelectionCandidateProvider
{
    /// <summary>
    /// Gets all <see cref="SelectionCandidate"/>s for a specific set of <see cref="Requirements"/> sorted from best to worst.
    /// </summary>
    IReadOnlyList<SelectionCandidate> GetSortedCandidates(Requirements requirements);

    /// <summary>
    /// Retrieves the original <see cref="Implementation"/> an <see cref="ImplementationSelection"/> was based ofF.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The <paramref name="implementationSelection"/> was not provided by <see cref="GetSortedCandidates"/>.</exception>
    Implementation LookupOriginalImplementation(ImplementationSelection implementationSelection);

    /// <summary>
    /// A list of feeds that could not be downloaded along with the exceptions describing the problems.
    /// </summary>
    IReadOnlyDictionary<FeedUri, Exception> FailedFeeds { get; }

    /// <summary>
    /// Clears any in-memory caches.
    /// </summary>
    void Clear();
}
