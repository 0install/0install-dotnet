// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Common base class for representing a single run of a solver.
/// </summary>
/// <param name="requirements">The requirements to satisfy.</param>
/// <param name="candidateProvider">Generates <see cref="SelectionCandidate"/>s for the solver to choose from.</param>
/// <remarks>This is intended to be used by private classes within <see cref="ISolver"/> implementations to hold state during a run of <see cref="ISolver.Solve"/>.</remarks>
public abstract class SolverRunBase(Requirements requirements, ISelectionCandidateProvider candidateProvider)
{
    private readonly Requirements _requirements = requirements.ForCurrentSystem();

    /// <summary>
    /// Generates <see cref="SelectionCandidate"/>s for the solver to choose from.
    /// </summary>
    protected readonly ISelectionCandidateProvider CandidateProvider = candidateProvider;

    /// <summary>
    /// Used to iteratively construct the selections to be returned.
    /// </summary>
    protected Selections Selections = new()
    {
        InterfaceUri = requirements.InterfaceUri,
        Command = requirements.Command
    };

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
    /// Tries to fulfill the specified solver demand. Adds the result to <see cref="Selections"/> if successful.
    /// </summary>
    /// <param name="demand">The demand to fulfill.</param>
    /// <returns><c>true</c> if the demand could be met, <c>false</c> if not.</returns>
    protected abstract bool TryFulfill(SolverDemand demand);

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
    protected IEnumerable<SolverDemand> DemandsFor(Command command, FeedUri interfaceUri)
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
