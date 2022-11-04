// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;

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

    /// <summary>
    /// Gets all <see cref="SelectionCandidate"/>s that are compatible with the specified <paramref name="selections"/>.
    /// </summary>
    public IEnumerable<SelectionCandidate> CandidatesCompatibleWith(Selections selections)
        => Candidates.Where(candidate => candidate.IsSuitable && !Conflicts(candidate.Implementation, selections));

    private bool Conflicts(Implementation implementation, Selections selections)
        => Conflicts(implementation.Architecture.Cpu, selections.Implementations)
        || Conflicts(implementation, selections.RestrictionsFor(Requirements.InterfaceUri))
        || Conflicts(implementation.GetEffectiveRestrictions(), selections);

    private static bool Conflicts(Cpu cpu, ICollection<ImplementationSelection> implementations)
        => cpu.Is32Bit() && implementations.Any(x => x.Architecture.Cpu.Is64Bit())
        || cpu.Is64Bit() && implementations.Any(x => x.Architecture.Cpu.Is32Bit());

    private static bool Conflicts(Implementation implementation, IEnumerable<Restriction> restrictions)
    {
        string? candidateDistribution = (implementation as ExternalImplementation)?.Distribution;
        foreach (var restriction in restrictions)
        {
            if (!restriction.Versions?.Match(implementation.Version) ?? false) return true;
            if (candidateDistribution != null && !restriction.Distributions.ContainsOrEmpty(candidateDistribution)) return true;
        }

        return false;
    }

    private static bool Conflicts(IEnumerable<Restriction> restrictions, Selections selections)
    {
        foreach (var restriction in restrictions)
        {
            if (selections.GetImplementation(restriction.InterfaceUri) is {} existingImplementation)
            {
                if (!restriction.Versions?.Match(existingImplementation.Version) ?? false) return true;
                if (existingImplementation.Distribution is {} distribution && !restriction.Distributions.ContainsOrEmpty(distribution)) return true;
            }
        }

        return false;
    }
}
