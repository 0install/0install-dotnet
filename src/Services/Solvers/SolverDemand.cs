// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// A demand used by <see cref="ISolver"/>s internally. Wrapper for <see cref="Requirements"/> that holds <see cref="SelectionCandidate"/>s.
/// </summary>
/// <param name="Requirements">The requirements.</param>
/// <param name="CandidateProvider">Generates <see cref="SelectionCandidate"/>s for the <paramref name="Requirements"/>.</param>
/// <param name="Importance">Describes how important the demand is (i.e. whether ignoring it is an option).</param>
public sealed record SolverDemand(Requirements Requirements, ISelectionCandidateProvider CandidateProvider, Importance Importance = Importance.Essential)
{
    /// <summary>
    /// All candidates for the <see cref="Requirements"/>, including those that are not suitable.
    /// </summary>
    public IReadOnlyList<SelectionCandidate> Candidates { get; } = CandidateProvider.GetSortedCandidates(Requirements);
}
