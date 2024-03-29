// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Dispatch;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Helper functions for <see cref="ISolver"/> implementations.
/// </summary>
public static class SolverUtils
{
    /// <summary>
    /// Turns <see cref="SelectionCandidate"/>s into <see cref="ImplementationSelection"/>s.
    /// </summary>
    /// <param name="candidates">The selection candidates.</param>
    /// <param name="demand">The solver demand the candidates were chosen for.</param>
    [LinqTunnel]
    public static IEnumerable<ImplementationSelection> ToSelections(this IEnumerable<SelectionCandidate> candidates, SolverDemand demand)
        => candidates.Select(x => x.ToSelection(demand.Requirements, allCandidates: demand.Candidates));

    /// <summary>
    /// Turns a <see cref="SelectionCandidate"/> into a <see cref="ImplementationSelection"/>.
    /// </summary>
    /// <param name="candidate">The selection candidate.</param>
    /// <param name="requirements">The requirements the candidate was chosen for.</param>
    /// <param name="allCandidates">All candidates that were considered for selection (including <paramref name="candidate"/>). These are used to present the user with possible alternatives.</param>
    public static ImplementationSelection ToSelection(this SelectionCandidate candidate, Requirements requirements, IReadOnlyList<SelectionCandidate> allCandidates)
    {
        #region Sanity checks
        if (candidate == null) throw new ArgumentNullException(nameof(candidate));
        if (allCandidates == null) throw new ArgumentNullException(nameof(allCandidates));
        if (requirements == null) throw new ArgumentNullException(nameof(requirements));
        #endregion

        var implementation = candidate.Implementation;
        var selection = new ImplementationSelection(allCandidates)
        {
            ID = implementation.ID,
            LocalPath = implementation.LocalPath,
            ManifestDigest = implementation.ManifestDigest,
            Architecture = implementation.Architecture,
            Version = implementation.Version,
            Released = implementation.Released,
            Stability = candidate.EffectiveStability,
            License = implementation.License,
            UnknownAttributes = implementation.UnknownAttributes,
            UnknownElements = implementation.UnknownElements,
            InterfaceUri = requirements.InterfaceUri,
            FromFeed = candidate.FeedUri != requirements.InterfaceUri ? candidate.FeedUri : null,
            QuickTestFile = (implementation as ExternalImplementation)?.QuickTestFile,
            Bindings = {implementation.Bindings.CloneElements()}
        };
        selection.AddDependencies(requirements, from: candidate.Implementation);
        selection.AddCommand(requirements, from: candidate.Implementation);
        return selection;
    }

    /// <summary>
    /// Transfers <see cref="Dependency"/>s from one <see cref="IDependencyContainer"/> to another.
    /// </summary>
    /// <param name="target">The <see cref="IDependencyContainer"/> to add the <see cref="Dependency"/>s to.</param>
    /// <param name="requirements">The requirements which restrict which <see cref="Dependency"/>s are applicable.</param>
    /// <param name="from">The <see cref="IDependencyContainer"/> to get the <see cref="Dependency"/>s to.</param>
    public static void AddDependencies(this IDependencyContainer target, Requirements requirements, IDependencyContainer from)
    {
        #region Sanity checks
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (requirements == null) throw new ArgumentNullException(nameof(requirements));
        if (from == null) throw new ArgumentNullException(nameof(from));
        #endregion

        target.Dependencies.Add(from.Dependencies.Where(x => x.IsApplicable(requirements)).CloneElements());
        target.Restrictions.Add(from.Restrictions.Where(x => x.IsApplicable(requirements)).CloneElements());
    }

    /// <summary>
    /// Adds a <see cref="Command"/> specified in an <see cref="Implementation"/> to a <see cref="ImplementationSelection"/>.
    /// </summary>
    /// <param name="selection">The <see cref="ImplementationSelection"/> to add the <see cref="Command"/> to.</param>
    /// <param name="requirements">The requirements specifying which <see cref="Requirements.Command"/> to extract.</param>
    /// <param name="from">The <see cref="Implementation"/> to get the <see cref="Command"/> from.</param>
    /// <returns>The <see cref="Command"/> that was added to <paramref name="selection"/>; <c>null</c> if none.</returns>
    [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "This method explicitly transfers information from an Implementation to an ImplementationSelection.")]
    public static Command? AddCommand(this ImplementationSelection selection, Requirements requirements, Implementation from)
    {
        #region Sanity checks
        if (selection == null) throw new ArgumentNullException(nameof(selection));
        if (requirements == null) throw new ArgumentNullException(nameof(requirements));
        if (from == null) throw new ArgumentNullException(nameof(from));
        #endregion

        Debug.Assert(requirements.Command != null);
        var command = from[requirements.Command];
        if (command == null) return null;

        var newCommand = new Command
        {
            Name = command.Name,
            Path = command.Path,
            Arguments = {command.Arguments.CloneElements()},
            Bindings = {command.Bindings.CloneElements()},
            WorkingDir = command.WorkingDir?.Clone(),
            Runner = command.Runner?.CloneRunner()
        };
        newCommand.AddDependencies(requirements, from: command);

        selection.Commands.Add(newCommand);
        return newCommand;
    }

    /// <summary>
    /// Adds the version restriction from <paramref name="source"/> to the <paramref name="requirements"/>.
    /// </summary>
    public static void AddRestriction(this Requirements requirements, Restriction source)
    {
        #region Sanity checks
        if (requirements == null) throw new ArgumentNullException(nameof(requirements));
        if (source == null) throw new ArgumentNullException(nameof(source));
        #endregion

        if (source.Versions != null)
            requirements.AddRestriction(source.InterfaceUri, source.Versions);
    }

    /// <summary>
    /// Adds the version restrictions from <paramref name="source"/> to the <paramref name="requirements"/>.
    /// </summary>
    public static void AddRestrictions(this Requirements requirements, Requirements source)
    {
        #region Sanity checks
        if (requirements == null) throw new ArgumentNullException(nameof(requirements));
        if (source == null) throw new ArgumentNullException(nameof(source));
        #endregion

        foreach (var restriction in source.ExtraRestrictions)
            requirements.AddRestriction(restriction.Key, restriction.Value);
    }

    /// <summary>
    /// Removes all <see cref="Restriction"/>s from <see cref="Selections"/>.
    /// </summary>
    public static void PurgeRestrictions(this Selections selections)
    {
        #region Sanity checks
        if (selections == null) throw new ArgumentNullException(nameof(selections));
        #endregion

        foreach (var implementation in selections.Implementations)
        {
            implementation.Restrictions.Clear();

            foreach (var command in implementation.Commands)
                command.Restrictions.Clear();
        }
    }

    /// <summary>
    /// Separates solver demands into buckets by importance.
    /// </summary>
    public static (List<SolverDemand> essential, List<SolverDemand> recommended) BucketizeImportance(this IEnumerable<SolverDemand> demands)
    {
        List<SolverDemand> essential = [], recommended = [];
        demands.Bucketize(x => x.Importance)
               .Add(Importance.Essential, essential)
               .Add(Importance.Recommended, recommended)
               .Run();
        return (essential, recommended);
    }
}
