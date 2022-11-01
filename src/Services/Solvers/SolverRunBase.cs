// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Common base class for representing a single run of a solver.
/// </summary>
/// <remarks>This is intended to be used by private classes within <see cref="ISolver"/> implementations to hold state during a run of <see cref="ISolver.Solve"/>.</remarks>
public abstract class SolverRunBase
{
    private readonly Requirements _requirements;

    /// <summary>
    /// Generates <see cref="SelectionCandidate"/>s for the solver to choose from.
    /// </summary>
    protected readonly ISelectionCandidateProvider CandidateProvider;

    /// <summary>
    /// Used to iteratively construct the selections to be returned.
    /// </summary>
    protected Selections Selections;

    /// <summary>
    /// Creates a new solver run.
    /// </summary>
    /// <param name="requirements">The requirements to satisfy.</param>
    /// <param name="candidateProvider">Generates <see cref="SelectionCandidate"/>s for the solver to choose from.</param>
    protected SolverRunBase(Requirements requirements, ISelectionCandidateProvider candidateProvider)
    {
        if (requirements == null) throw new ArgumentNullException(nameof(requirements));
        if (requirements.InterfaceUri == null) throw new ArgumentException(Resources.MissingInterfaceUri, nameof(requirements));
        _requirements = requirements.ForCurrentSystem();

        CandidateProvider = candidateProvider;

        Selections = new Selections
        {
            InterfaceUri = requirements.InterfaceUri,
            Command = requirements.Command
        };
    }

    /// <summary>
    /// Provides <see cref="Selections"/> that satisfy the specified <see cref="Requirements"/>.
    /// </summary>
    /// <returns>The selected <see cref="ImplementationSelection"/>s.</returns>
    /// <exception cref="SolverException">The solver was unable to provide <see cref="Selections"/> that fulfill the <see cref="Requirements"/>.</exception>
    public Selections Solve()
    {
        try
        {
            if (!TryFulfill(Demand(_requirements)))
            {
                CandidateProvider.FailedFeeds.Values.FirstOrDefault()?.Rethrow();
                throw new SolverException("No solution found");
            }
            return Selections;
        }
        finally
        {
            Selections.PurgeRestrictions();
            Selections.Implementations.Sort();
        }
    }

    /// <summary>
    /// Tries to fulfill the specified <paramref name="demand"/>. Adds the result to <see cref="Selections"/> if successful.
    /// </summary>
    /// <returns><c>true</c> if the demand could be met, <c>false</c> if not.</returns>
    protected bool TryFulfill(SolverDemand demand)
    {
        var candidates = GetCompatibleCandidates(demand);

        var existingSelection = Selections.GetImplementation(demand.Requirements.InterfaceUri);
        if (existingSelection == null)
        { // Try to make new selection
            return TryFulfill(demand, candidates);
        }
        else
        { // Try to use existing selection
            // Ensure existing selection is one of the compatible candidates
            if (candidates.All(x => x.Implementation.ID != existingSelection.ID)) return false;

            if (!existingSelection.ContainsCommand(demand.Requirements.Command ?? Command.NameRun))
            { // Add additional command to selection if needed
                var command = existingSelection.AddCommand(demand.Requirements, from: CandidateProvider.LookupOriginalImplementation(existingSelection));
                return (command == null) || TryFulfill(DemandsFor(command, demand.Requirements.InterfaceUri));
            }
            return true;
        }
    }

    /// <summary>
    /// Gets all <see cref="SelectionCandidate"/>s for the <paramref name="demand"/> that are compatible with the current <see cref="Selections"/> state.
    /// </summary>
    private IEnumerable<SelectionCandidate> GetCompatibleCandidates(SolverDemand demand) => demand.Candidates.Where(candidate =>
    {
        if (!candidate.IsSuitable) return false;

        var nativeImplementation = candidate.Implementation as ExternalImplementation;

        // Ensure the candidate does not conflict with restrictions of existing selections
        foreach (var restriction in Selections.RestrictionsFor(demand.Requirements.InterfaceUri))
        {
            // Prevent mixing of 32-bit and 64-bit binaries
            if (candidate.Implementation.Architecture.Cpu.Is32Bit() && Selections.Is64Bit) return false;
            if (candidate.Implementation.Architecture.Cpu.Is64Bit() && Selections.Is32Bit) return false;

            if (restriction.Versions != null && !restriction.Versions.Match(candidate.Version)) return false;
            if (nativeImplementation != null && !restriction.Distributions.ContainsOrEmpty(nativeImplementation.Distribution)) return false;
        }

        // Ensure the existing selections do not conflict with restrictions of the candidate
        foreach (var restriction in candidate.Implementation.GetEffectiveRestrictions())
        {
            if (Selections.GetImplementation(restriction.InterfaceUri) is {} selection)
            {
                if (restriction.Versions != null && !restriction.Versions.Match(selection.Version)) return false;
                if (nativeImplementation != null && !restriction.Distributions.ContainsOrEmpty(nativeImplementation.Distribution)) return false;
            }
        }

        return true;
    });

    /// <summary>
    /// Tries to fulfill the specified solver demand. Adds the result to <see cref="Selections"/> if successful.
    /// </summary>
    /// <param name="demand">The demand to fulfill.</param>
    /// <param name="candidates">The candidates to consider for fulfilling the demand.</param>
    /// <returns><c>true</c> if the demand could be met, <c>false</c> if not.</returns>
    protected abstract bool TryFulfill(SolverDemand demand, IEnumerable<SelectionCandidate> candidates);

    /// <summary>
    /// Tries to fulfill the specified <paramref name="demands"/>. Adds the results to <see cref="Selections"/> if successful.
    /// </summary>
    /// <returns><c>true</c> if all <see cref="Importance.Essential"/> demands could be met, <c>false</c> if not.</returns>
    protected abstract bool TryFulfill(IEnumerable<SolverDemand> demands);

    /// <summary>
    /// Generates <see cref="SolverDemand"/>s for the dependencies specified by an <see cref="ImplementationSelection"/>.
    /// </summary>
    /// <param name="selection">The selection to scan for dependencies.</param>
    /// <param name="requirements">Requirements to inherit into the demands.</param>
    protected IEnumerable<SolverDemand> DemandsFor(ImplementationSelection selection, Requirements requirements)
    {
        foreach (var demand in selection.Dependencies.SelectMany(DemandsFor))
            yield return demand;

        foreach (var demands in DemandsFor(selection.Bindings, selection.InterfaceUri))
            yield return demands;

        if (selection[requirements.Command ?? Command.NameRun] is {} command)
        {
            foreach (var demand in DemandsFor(command, requirements.InterfaceUri))
                yield return demand;
        }
    }

    /// <summary>
    /// Generates <see cref="SolverDemand"/>s for the dependencies specified by a <see cref="Command"/>.
    /// </summary>
    /// <param name="command">The command to scan for dependencies.</param>
    /// <param name="interfaceUri">The interface URI of the feed providing the command.</param>
    private IEnumerable<SolverDemand> DemandsFor(Command command, FeedUri interfaceUri)
    {
        if (command.Runner != null)
        {
            var requirements = Require(command.Runner.InterfaceUri, command.Runner.Command);
            requirements.AddRestriction(command.Runner);
            yield return Demand(requirements);
        }

        foreach (var demand in command.Dependencies.SelectMany(DemandsFor))
            yield return demand;

        foreach (var demands in DemandsFor(command.Bindings, interfaceUri))
            yield return demands;
    }

    /// <summary>
    /// Generates <see cref="SolverDemand"/>s for the <paramref name="dependency"/> and its bindings.
    /// </summary>
    private IEnumerable<SolverDemand> DemandsFor(Dependency dependency)
    {
        {
            var requirements = Require(dependency.InterfaceUri, command: "");
            requirements.Distributions.Add(dependency.Distributions);
            requirements.AddRestriction(dependency);
            yield return Demand(requirements, dependency.Importance);
        }

        foreach (var demands in DemandsFor(dependency.Bindings, dependency.InterfaceUri, dependency.Importance))
            yield return demands;
    }

    /// <summary>
    /// Generates <see cref="Requirements"/> for <see cref="Binding"/>s.
    /// </summary>
    /// <param name="bindings">The bindings to generate requirement for.</param>
    /// <param name="interfaceUri">The interface URI of the feed providing the bindings.</param>
    /// <param name="importance">Describes how important the demand is (i.e. whether ignoring it is an option).</param>
    private IEnumerable<SolverDemand> DemandsFor(IEnumerable<Binding> bindings, FeedUri interfaceUri, Importance importance = Importance.Essential)
        => bindings.OfType<ExecutableInBinding>()
                   .Select(binding => Require(interfaceUri, binding.Command))
                   .Select(requirements => Demand(requirements, importance));

    /// <summary>
    /// Creates a new solver demand.
    /// </summary>
    /// <param name="requirements">The requirements.</param>
    /// <param name="importance">Describes how important the demand is (i.e. whether ignoring it is an option).</param>
    private SolverDemand Demand(Requirements requirements, Importance importance = Importance.Essential)
        => new(requirements, CandidateProvider, importance);

    /// <summary>
    /// Creates new <see cref="Requirements"/>, inheriting restrictions from <see cref="_requirements"/>.
    /// </summary>
    /// <param name="interfaceUri">The URI or local path (must be absolute) to the interface to solve the dependencies for.</param>
    /// <param name="command">The name of the command in the implementation to execute. Will default to <see cref="Command.NameRun"/> or <see cref="Command.NameCompile"/> if <c>null</c>. Will not try to find any command if set to <see cref="string.Empty"/>.</param>
    private Requirements Require(FeedUri interfaceUri, string? command)
    {
        var requirements = new Requirements(interfaceUri, command ?? Command.NameRun, _requirements.Architecture)
        {
            Languages = {_requirements.Languages}
        };
        requirements.AddRestrictions(_requirements);
        return requirements;
    }
}
