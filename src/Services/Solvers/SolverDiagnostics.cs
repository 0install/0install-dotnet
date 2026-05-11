// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Generates a human-readable explanation of why an <see cref="ISolver"/> was unable to satisfy the <see cref="Requirements"/>.
/// </summary>
internal sealed class SolverDiagnostics
{
    private readonly Requirements _rootRequirements;
    private readonly IReadOnlyDictionary<FeedUri, Exception> _failedFeeds;
    private readonly Dictionary<FeedUri, Component> _components;

    private SolverDiagnostics(
        Requirements rootRequirements,
        IReadOnlyDictionary<FeedUri, IReadOnlyList<SelectionCandidate>> roles,
        IReadOnlyDictionary<FeedUri, Exception> failedFeeds)
    {
        _rootRequirements = rootRequirements;
        _failedFeeds = failedFeeds;
        _components = roles.ToDictionary(x => x.Key, x => new Component(x.Key, x.Value));
    }

    /// <summary>
    /// Builds a diagnostic message describing why no solution could be found.
    /// </summary>
    /// <param name="rootRequirements">The user-supplied requirements being solved.</param>
    /// <param name="roles">The interfaces the solver visited, with their candidate lists.</param>
    /// <param name="failedFeeds">Feeds that could not be downloaded.</param>
    public static string Build(
        Requirements rootRequirements,
        IReadOnlyDictionary<FeedUri, IReadOnlyList<SelectionCandidate>> roles,
        IReadOnlyDictionary<FeedUri, Exception> failedFeeds)
    {
        var diagnostics = new SolverDiagnostics(rootRequirements, roles, failedFeeds);
        diagnostics.NoteFailedFeeds();
        diagnostics.ApplyUserRestrictions();
        diagnostics.ApplyRootCommand();
        diagnostics.VisitFromRoot();
        diagnostics.ApplyMachineGroupConstraint();
        diagnostics.PickOrphans();
        return diagnostics.Format();
    }

    private void NoteFailedFeeds()
    {
        foreach (var (uri, ex) in _failedFeeds)
        {
            if (_components.TryGetValue(uri, out var component))
                component.Notes.Add($"Main feed '{uri}' not available: {ex.Message}");
        }
    }

    private void ApplyUserRestrictions()
    {
        foreach (var (uri, range) in _rootRequirements.ExtraRestrictions)
        {
            if (!_components.TryGetValue(uri, out var component)) continue;
            component.Notes.Add($"User requested version {range}");
            string failReason = $"Incompatible with restriction: version {range}";
            component.FilterGood(c => range.Match(c.Version) ? null : failReason);
        }
    }

    private void ApplyRootCommand()
    {
        if (string.IsNullOrEmpty(_rootRequirements.Command)) return;
        if (!_components.TryGetValue(_rootRequirements.InterfaceUri, out var component)) return;

        string command = _rootRequirements.Command;
        component.FilterGood(c => c.Implementation.ContainsCommand(command) ? null : $"No {command} command");
    }

    /// <summary>
    /// Walks the dependency tree from the root and commits to a tentative selection per role. Restrictions from each committed selection are propagated to its dependency roles before they get visited, so each role's conflict-check sees a coherent partial solution.
    /// </summary>
    private void VisitFromRoot()
    {
        var visited = new HashSet<FeedUri>();
        var queue = new Queue<FeedUri>();
        queue.Enqueue(_rootRequirements.InterfaceUri);

        while (queue.Count > 0)
        {
            var uri = queue.Dequeue();
            if (!visited.Add(uri)) continue;
            if (!_components.TryGetValue(uri, out var component)) continue;

            CommitSelection(component, visited);

            if (component.Selected is not {} selected) continue;

            string commandName = uri == _rootRequirements.InterfaceUri
                ? _rootRequirements.Command ?? Command.NameRun
                : Command.NameRun;

            foreach (var dependency in selected.Implementation.Dependencies.Where(d => d.IsApplicable(_rootRequirements)))
            {
                ApplyDependency(component, selected, dependency);
                queue.Enqueue(dependency.InterfaceUri);
            }
            foreach (var restriction in selected.Implementation.Restrictions.Where(r => r.IsApplicable(_rootRequirements)))
                ApplyRestriction(component, selected, restriction);

            if (selected.Implementation.GetCommand(commandName) is {} command)
            {
                if (command.Runner is {} runner)
                {
                    ApplyRunner(component, selected, runner);
                    queue.Enqueue(runner.InterfaceUri);
                }
                foreach (var dependency in command.Dependencies.Where(d => d.IsApplicable(_rootRequirements)))
                {
                    ApplyDependency(component, selected, dependency);
                    queue.Enqueue(dependency.InterfaceUri);
                }
                foreach (var restriction in command.Restrictions.Where(r => r.IsApplicable(_rootRequirements)))
                    ApplyRestriction(component, selected, restriction);
            }
        }
    }

    /// <summary>
    /// Picks the highest-ranked candidate in <see cref="Component.Good"/> that doesn't itself conflict with the selections of already-decided roles, and demotes conflicting candidates to <see cref="Component.Bad"/>.
    /// </summary>
    private void CommitSelection(Component component, HashSet<FeedUri> decidedUris)
    {
        var stillGood = new List<SelectionCandidate>(component.Good.Count);
        foreach (var candidate in component.Good)
        {
            string? conflict = CheckOwnConflicts(component.InterfaceUri, candidate, decidedUris);
            if (conflict == null) stillGood.Add(candidate);
            else component.Bad.Add((candidate, conflict));
        }
        component.Good = stillGood;
        component.Selected = component.Good.FirstOrDefault();
    }

    private string? CheckOwnConflicts(FeedUri role, SelectionCandidate candidate, HashSet<FeedUri> decidedUris)
    {
        var impl = candidate.Implementation;
        string commandName = role == _rootRequirements.InterfaceUri
            ? _rootRequirements.Command ?? Command.NameRun
            : Command.NameRun;

        IEnumerable<Restriction> AllConstraints()
        {
            foreach (var d in impl.Dependencies.Where(d => d.IsApplicable(_rootRequirements))) yield return d;
            foreach (var r in impl.Restrictions.Where(r => r.IsApplicable(_rootRequirements))) yield return r;
            if (impl.GetCommand(commandName) is {} command)
            {
                foreach (var d in command.Dependencies.Where(d => d.IsApplicable(_rootRequirements))) yield return d;
                foreach (var r in command.Restrictions.Where(r => r.IsApplicable(_rootRequirements))) yield return r;
                if (command.Runner is {} runner) yield return runner;
            }
        }

        foreach (var constraint in AllConstraints())
        {
            if (!decidedUris.Contains(constraint.InterfaceUri)) continue;
            if (!_components.TryGetValue(constraint.InterfaceUri, out var depComponent)) continue;
            if (depComponent.Selected is not {} depSelected) continue;
            if (FormatRestriction(constraint) is not {} formatted) continue;
            if (RestrictionFails(depSelected, constraint))
                return $"Requires {constraint.InterfaceUri} {formatted}";
        }
        return null;
    }

    private void ApplyDependency(Component requiringComponent, SelectionCandidate requiringImpl, Dependency dependency)
    {
        if (!_components.TryGetValue(dependency.InterfaceUri, out var depComponent)) return;
        ApplyRestrictionToTarget(requiringComponent, requiringImpl, depComponent, dependency);

        foreach (var binding in dependency.Bindings.OfType<ExecutableInBinding>())
        {
            string command = binding.Command ?? Command.NameRun;
            depComponent.Notes.Add($"{requiringComponent.InterfaceUri} {requiringImpl.Version} requires '{command}' command");
            depComponent.FilterGood(c => c.Implementation.ContainsCommand(command) ? null : $"No {command} command");
        }
    }

    private void ApplyRestriction(Component requiringComponent, SelectionCandidate requiringImpl, Restriction restriction)
    {
        if (!_components.TryGetValue(restriction.InterfaceUri, out var depComponent)) return;
        ApplyRestrictionToTarget(requiringComponent, requiringImpl, depComponent, restriction);
    }

    private void ApplyRunner(Component requiringComponent, SelectionCandidate requiringImpl, Runner runner)
    {
        if (!_components.TryGetValue(runner.InterfaceUri, out var runnerComponent)) return;
        ApplyRestrictionToTarget(requiringComponent, requiringImpl, runnerComponent, runner);

        string command = runner.Command ?? Command.NameRun;
        runnerComponent.Notes.Add($"{requiringComponent.InterfaceUri} {requiringImpl.Version} requires '{command}' command");
        runnerComponent.FilterGood(c => c.Implementation.ContainsCommand(command) ? null : $"No {command} command");
    }

    private static void ApplyRestrictionToTarget(Component requiringComponent, SelectionCandidate requiringImpl, Component target, Restriction restriction)
    {
        if (FormatRestriction(restriction) is not {} formatted) return;
        target.Notes.Add($"{requiringComponent.InterfaceUri} {requiringImpl.Version} requires {formatted}");
        target.FilterGood(c => RestrictionFails(c, restriction) ? $"Incompatible with restriction: {formatted}" : null);
    }

    private static bool RestrictionFails(SelectionCandidate candidate, Restriction restriction)
    {
        if (restriction.Versions != null && !restriction.Versions.Match(candidate.Version))
            return true;
        if (restriction.Distributions.Count > 0)
        {
            string candidateDist = (candidate.Implementation as ExternalImplementation)?.Distribution ?? Restriction.DistributionZeroInstall;
            if (!restriction.Distributions.Contains(candidateDist))
                return true;
        }
        return false;
    }

    private static string? FormatRestriction(Restriction restriction)
    {
        var parts = new List<string>();
        if (restriction.Versions != null) parts.Add($"version {restriction.Versions}");
        parts.AddRange(restriction.Distributions.Select(distribution => $"distribution:{distribution}"));
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    private void ApplyMachineGroupConstraint()
    {
        // Find the first selected impl (in topological order from root) with a CPU group; treat that as the anchor.
        Component? anchor = null;
        CpuGroup anchorGroup = default;
        foreach (var component in _components.Values.OrderBy(c => c.InterfaceUri == _rootRequirements.InterfaceUri ? 0 : 1).ThenBy(c => c.InterfaceUri.ToString(), StringComparer.Ordinal))
        {
            if (component.Selected?.Implementation.Architecture.Cpu.GetGroup() is {} group)
            {
                anchor = component;
                anchorGroup = group;
                break;
            }
        }
        if (anchor == null) return;

        foreach (var component in _components.Values)
        {
            if (ReferenceEquals(component, anchor)) continue;
            component.FilterGood(c =>
            {
                var otherGroup = c.Implementation.Architecture.Cpu.GetGroup();
                return otherGroup is null || otherGroup == anchorGroup
                    ? null
                    : $"Can't use {c.Implementation.Architecture.Cpu} with selection of {anchor.InterfaceUri} ({anchor.Selected!.Implementation.Architecture.Cpu})";
            });
            if (component.Selected != null && !component.Good.Contains(component.Selected))
                component.Selected = component.Good.FirstOrDefault();
        }
    }

    private void PickOrphans()
    {
        foreach (var component in _components.Values)
        {
            if (component.Selected == null && component.Good.Count > 0 && component.Bad.Count == 0)
                component.Selected = component.Good.FirstOrDefault();
        }
    }

    private string Format()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Can't find all required implementations:");
        foreach (var component in _components.Values.OrderBy(c => c.InterfaceUri.ToString(), StringComparer.Ordinal))
            component.AppendTo(builder);
        return builder.ToString().TrimEnd();
    }

    private sealed class Component(FeedUri interfaceUri, IReadOnlyList<SelectionCandidate> candidates)
    {
        public FeedUri InterfaceUri { get; } = interfaceUri;
        public SelectionCandidate? Selected { get; set; }
        public List<SelectionCandidate> Good { get; set; } = candidates.Where(c => c.IsSuitable).ToList();
        public List<(SelectionCandidate Candidate, string Reason)> Bad { get; } = candidates.Where(c => !c.IsSuitable)
                                                                                            .Select(c => (c, c.Notes ?? "Unsuitable"))
                                                                                            .ToList();
        public List<string> Notes { get; } = [];

        public void FilterGood(Func<SelectionCandidate, string?> getReason)
        {
            var stillGood = new List<SelectionCandidate>(Good.Count);
            foreach (var candidate in Good)
            {
                if (getReason(candidate) is {} reason) Bad.Add((candidate, reason));
                else stillGood.Add(candidate);
            }
            Good = stillGood;
        }

        public void AppendTo(StringBuilder builder)
        {
            string outcome = Selected is {} sel ? $"{sel.Version} ({sel.Implementation.ID})" : "(problem)";
            builder.Append("- ")
                   .Append(InterfaceUri)
                   .Append(" -> ")
                   .AppendLine(outcome);

            foreach (string note in Notes)
                builder.Append("    ").AppendLine(note);

            if (Selected != null) return;

            if (candidates.Count == 0)
            {
                builder.AppendLine("    No known implementations at all");
                return;
            }

            if (Bad.Count == 0) return;

            bool allUnusable = Good.Count == 0 && Bad.All(b => !b.Candidate.IsSuitable);
            builder.AppendLine(allUnusable ? "    No usable implementations:" : "    Rejected candidates:");
            foreach ((var candidate, string reason) in Bad.OrderByDescending(x => x.Candidate.Version))
            {
                builder.Append("      ")
                       .Append(candidate.Implementation.ID)
                       .Append(" (")
                       .Append(candidate.Version)
                       .Append("): ")
                       .AppendLine(reason);
            }
        }
    }
}
