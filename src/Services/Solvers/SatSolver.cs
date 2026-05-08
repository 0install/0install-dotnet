// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.SatSolver;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;

namespace ZeroInstall.Services.Solvers;

/// <summary>
/// Uses a boolean satisfiability solver to solve <see cref="Requirements"/>.
/// </summary>
/// <param name="candidateProvider">Generates <see cref="SelectionCandidate"/>s for the solver to choose from.</param>
public class SatSolver(ISelectionCandidateProvider candidateProvider) : ISolver
{
    /// <inheritdoc/>
    public Selections Solve(Requirements requirements)
    {
        requirements = requirements.ForCurrentSystem();
        Log.Info($"Running SAT Solver for {requirements}");
        return new SolverRun(requirements, candidateProvider).Solve();
    }

    /// <summary>Discriminated key for SAT variables: an implementation candidate or a CPU group sentinel.</summary>
    private abstract record NodeKey;
    private sealed record ImplNode(SelectionCandidate Candidate) : NodeKey;
    private sealed record CpuGroupNode(CpuGroup Group) : NodeKey;

    private sealed class RoleData
    {
        public required FeedUri InterfaceUri { get; init; }
        public required Requirements BaselineRequirements { get; init; }
        public required IReadOnlyList<SelectionCandidate> Candidates { get; init; }
        public AtMostOneConstraint<NodeKey>? AtMostOne { get; set; }
    }

    private class SolverRun(Requirements requirements, ISelectionCandidateProvider candidateProvider) : SolverRunBase(requirements, candidateProvider)
    {
        private readonly Requirements _rootRequirements = requirements.ForCurrentSystem();
        private readonly SatProblem<NodeKey> _problem = new();
        private readonly Dictionary<FeedUri, RoleData> _roles = [];
        private readonly HashSet<CpuGroup> _cpuGroups = [];
        private readonly Queue<FeedUri> _encodingQueue = new();
        private readonly Queue<(SelectionCandidate Candidate, string CommandName)> _commandQueue = new();
        private readonly HashSet<(SelectionCandidate, string)> _encodedCommands = [];
        private readonly Dictionary<SelectionCandidate, RoleData> _candidateRole = [];

        protected override bool TryFulfill(SolverDemand rootDemand)
        {
            Selections = new() {InterfaceUri = rootDemand.Requirements.InterfaceUri, Command = rootDemand.Requirements.Command};

            BuildProblem(rootDemand);
            if (_problem.Solve(Decide) is {} model)
            {
                ExtractSelections(model, rootDemand.Requirements);
                return true;
            }

            return false;
        }

        private void BuildProblem(SolverDemand rootDemand)
        {
            var rootRole = VisitRole(rootDemand.Requirements);

            // Root: at least one candidate matching the requested command (and version, distribution, etc.) must be selected.
            var rootMatching = rootRole.Candidates.Where(c => MatchesRequirements(c, rootDemand.Requirements)).ToList();
            if (rootMatching.Count == 0)
            {
                _problem.AddClause(); // empty clause -> unsat
                return;
            }
            _problem.AddClause(rootMatching.Select(LitFor));

            // Queue command-level encoding for the root requirement: each matching root candidate may contribute the requested command's deps/runner/restricts/bindings.
            if (!string.IsNullOrEmpty(rootDemand.Requirements.Command))
            {
                foreach (var candidate in rootMatching)
                    EnqueueCommand(candidate, rootDemand.Requirements.Command);
            }

            // Drain both queues. Either may add work to the other (encoding deps visits new roles; command encoding for runners/bindings queues new commands).
            while (_encodingQueue.Count > 0 || _commandQueue.Count > 0)
            {
                while (_encodingQueue.Count > 0)
                {
                    var iface = _encodingQueue.Dequeue();
                    var role = _roles[iface];
                    foreach (var candidate in role.Candidates)
                        EncodeCandidateImplLevel(candidate, role.BaselineRequirements);
                }
                while (_commandQueue.Count > 0)
                {
                    var (candidate, commandName) = _commandQueue.Dequeue();
                    EncodeCandidateCommand(candidate, commandName);
                }
            }

            // Cross-role CPU group constraint: e.g. AtMostOne(32, 64).
            if (_cpuGroups.Count >= 2)
                _problem.AtMostOne(_cpuGroups.Select(g => Literal.Of<NodeKey>(new CpuGroupNode(g))));
        }

        private void EnqueueCommand(SelectionCandidate candidate, string? commandName)
        {
            if (string.IsNullOrEmpty(commandName)) return;
            var key = (candidate, commandName);
            if (_encodedCommands.Add(key))
                _commandQueue.Enqueue((candidate, commandName));
        }

        private RoleData VisitRole(Requirements reqs)
        {
            if (_roles.TryGetValue(reqs.InterfaceUri, out var existing)) return existing;

            // Use a baseline requirements with no command and no dependency-specific version restriction so we capture every candidate of the interface that's broadly suitable. Per-dependency filtering is applied via clauses, not via the candidate set.
            var baseline = Require(reqs.InterfaceUri, command: "");
            var candidates = CandidateProvider.GetSortedCandidates(baseline)
                .Where(c => c.IsSuitable)
                .ToList();

            var role = new RoleData
            {
                InterfaceUri = reqs.InterfaceUri,
                BaselineRequirements = baseline,
                Candidates = candidates
            };
            _roles[reqs.InterfaceUri] = role;

            foreach (var candidate in candidates)
            {
                _problem.AddVariable(new ImplNode(candidate));
                _candidateRole[candidate] = role;
            }

            if (candidates.Count > 1)
                role.AtMostOne = _problem.AtMostOne(candidates.Select(LitFor));

            // Bind each architecture-specific candidate to its CPU group.
            foreach (var candidate in candidates)
            {
                if (candidate.Implementation.Architecture.Cpu.GetGroup() is { } group)
                {
                    _cpuGroups.Add(group);
                    _problem.AddVariable(new CpuGroupNode(group));
                    _problem.Implies(LitFor(candidate), Literal.Of<NodeKey>(new CpuGroupNode(group)));
                }
            }

            _encodingQueue.Enqueue(reqs.InterfaceUri);
            return role;
        }

        private void EncodeCandidateImplLevel(SelectionCandidate candidate, Requirements roleReqs)
        {
            var candidateLit = LitFor(candidate);
            var impl = candidate.Implementation;

            foreach (var dependency in impl.Dependencies.Where(x => x.IsApplicable(roleReqs)))
                EncodeDependency(candidateLit, dependency);
            foreach (var restriction in impl.Restrictions.Where(x => x.IsApplicable(roleReqs)))
                EncodeRestriction(candidateLit, restriction);
            foreach (var binding in impl.Bindings.OfType<ExecutableInBinding>())
                EncodeSelfBinding(candidateLit, roleReqs.InterfaceUri, binding);
        }

        private void EncodeCandidateCommand(SelectionCandidate candidate, string commandName)
        {
            if (!candidate.Implementation.ContainsCommand(commandName)) return;
            if (candidate.Implementation[commandName] is not {} command) return;

            var candidateLit = LitFor(candidate);
            var roleReqs = _candidateRole.TryGetValue(candidate, out var role) ? role.BaselineRequirements : _rootRequirements;

            if (command.Runner != null) EncodeRunner(candidateLit, command.Runner);
            foreach (var dependency in command.Dependencies.Where(x => x.IsApplicable(roleReqs)))
                EncodeDependency(candidateLit, dependency);
            foreach (var restriction in command.Restrictions.Where(x => x.IsApplicable(roleReqs)))
                EncodeRestriction(candidateLit, restriction);
            foreach (var binding in command.Bindings.OfType<ExecutableInBinding>())
                EncodeSelfBinding(candidateLit, role?.InterfaceUri ?? _rootRequirements.InterfaceUri, binding);
        }

        private void EncodeDependency(Literal<NodeKey> parent, Dependency dependency)
        {
            var requirements = Require(dependency.InterfaceUri, command: "");
            requirements.Distributions.Add(dependency.Distributions);
            if (dependency.Versions != null) requirements.AddRestriction(dependency.InterfaceUri, dependency.Versions);

            var depRole = VisitRole(requirements);
            var matching = depRole.Candidates.Where(c => MatchesRequirements(c, requirements)).ToList();

            if (dependency.Importance == Importance.Essential)
                _problem.AddClause([parent.Negate(), ..matching.Select(LitFor)]);

            // Violator exclusion (applies for both Essential and Recommended deps).
            foreach (var violator in depRole.Candidates.Except(matching))
                _problem.AddClause(parent.Negate(), LitFor(violator).Negate());

            // ExecutableInBindings on the dependency require specific commands on the chosen dependency candidate.
            foreach (var binding in dependency.Bindings.OfType<ExecutableInBinding>())
            {
                if (dependency.Importance != Importance.Essential) continue; // recommended-dependency command bindings not encoded; over-permissive but safe

                string bindingCommand = binding.Command ?? Command.NameRun;
                var matchingCommands = matching.Where(c => c.Implementation.ContainsCommand(bindingCommand)).ToList();
                _problem.AddClause([parent.Negate(), ..matchingCommands.Select(LitFor)]);

                // The chosen dependency candidate's binding command is now active: queue it for command-level encoding.
                foreach (var command in matchingCommands)
                    EnqueueCommand(command, bindingCommand);
            }
        }

        private void EncodeRestriction(Literal<NodeKey> parent, Restriction restriction)
        {
            var depReqs = Require(restriction.InterfaceUri, command: "");
            depReqs.Distributions.Add(restriction.Distributions);
            if (restriction.Versions != null) depReqs.AddRestriction(restriction.InterfaceUri, restriction.Versions);

            var depRole = VisitRole(depReqs);

            // <restricts> doesn't require selection: it only excludes violators when this candidate is selected.
            foreach (var violator in depRole.Candidates.Where(c => !MatchesRequirements(c, depReqs)))
                _problem.AddClause(parent.Negate(), LitFor(violator).Negate());
        }

        private void EncodeRunner(Literal<NodeKey> parent, Runner runner)
        {
            // A <runner> on a command is essentially a dependency on a specific command of the runner interface.
            string runnerCommand = runner.Command ?? Command.NameRun;
            var runnerReqs = Require(runner.InterfaceUri, command: runnerCommand);
            runnerReqs.Distributions.Add(runner.Distributions);
            if (runner.Versions != null)
                runnerReqs.AddRestriction(runner.InterfaceUri, runner.Versions);

            var role = VisitRole(runnerReqs);
            var matching = role.Candidates.Where(c => MatchesRequirements(c, runnerReqs)).ToList();

            _problem.AddClause([parent.Negate(), ..matching.Select(LitFor)]);

            foreach (var violator in role.Candidates.Except(matching))
                _problem.AddClause(parent.Negate(), LitFor(violator).Negate());

            // The chosen runner candidate's command is now active: queue it for command-level encoding so the runner's own deps/runner/bindings get encoded.
            foreach (var candidate in matching)
                EnqueueCommand(candidate, runnerCommand);
        }

        private void EncodeSelfBinding(Literal<NodeKey> parent, FeedUri iface, ExecutableInBinding binding)
        {
            string commandName = binding.Command ?? Command.NameRun;

            var role = VisitRole(Require(iface, command: ""));
            var matching = role.Candidates.Where(c => c.Implementation.ContainsCommand(commandName)).ToList();

            _problem.AddClause([parent.Negate(), ..matching.Select(LitFor)]);

            foreach (var violator in role.Candidates.Except(matching))
                _problem.AddClause(parent.Negate(), LitFor(violator).Negate());

            // The bound command is now active on the chosen candidate: queue it for command-level encoding.
            foreach (var candidate in matching)
                EnqueueCommand(candidate, commandName);
        }

        // Walks the frontier between decided and undecided roles in the dependency tree from the root, then returns the best-undecided candidate of the most-constrained role (fewest remaining candidates). This matches the "best version for elements further up the dependency chain" heuristic from BacktrackingSolver / OCaml: a role that constrains another (e.g. via a version range on a transitive dep) ends up with fewer remaining candidates, so it gets picked first — locking in its best version before downstream choices over-constrain it.
        private Literal<NodeKey>? Decide()
        {
            RoleData? bestRole = null;
            int bestCount = int.MaxValue;
            CollectFrontier(_rootRequirements.InterfaceUri, new HashSet<FeedUri>(), ref bestRole, ref bestCount);
            return bestRole == null ? null : BestUndecidedFor(bestRole);
        }

        private Literal<NodeKey>? BestUndecidedFor(RoleData role)
        {
            if (role.AtMostOne != null) return role.AtMostOne.BestUndecided();
            if (role.Candidates.Count == 1)
            {
                var only = LitFor(role.Candidates[0]);
                if (_problem.GetValue(only) is null) return only;
            }
            return null;
        }

        private int UndecidedCount(RoleData role)
        {
            int count = 0;
            foreach (var candidate in role.Candidates)
                if (_problem.GetValue(LitFor(candidate)) is null) count++;
            return count;
        }

        private void CollectFrontier(FeedUri iface, HashSet<FeedUri> seen, ref RoleData? bestRole, ref int bestCount)
        {
            if (!seen.Add(iface)) return;
            if (!_roles.TryGetValue(iface, out var role)) return;

            if (BestUndecidedFor(role) != null)
            {
                int count = UndecidedCount(role);
                if (count < bestCount) { bestCount = count; bestRole = role; }
                return; // role itself is on the frontier; don't reach past it into transitive deps
            }

            if (role.Candidates.FirstOrDefault(c => _problem.GetValue(LitFor(c)) == true) is not { } selected) return;

            foreach (var dependency in selected.Implementation.Dependencies.Where(x => x.IsApplicable(role.BaselineRequirements)))
                CollectFrontier(dependency.InterfaceUri, seen, ref bestRole, ref bestCount);

            foreach (var command in selected.Implementation.Commands)
            {
                if (command.Runner != null)
                    CollectFrontier(command.Runner.InterfaceUri, seen, ref bestRole, ref bestCount);
                foreach (var dependency in command.Dependencies.Where(x => x.IsApplicable(role.BaselineRequirements)))
                    CollectFrontier(dependency.InterfaceUri, seen, ref bestRole, ref bestCount);
            }
        }

        private void ExtractSelections(Model<NodeKey> model, Requirements rootRequirements)
        {
            Visit(new SolverDemand(rootRequirements, CandidateProvider));

            void Visit(SolverDemand demand)
            {
                var iface = demand.Requirements.InterfaceUri;
                if (!_roles.TryGetValue(iface, out var role)) return;
                var selectedCandidate = role.Candidates.FirstOrDefault(c => model[new ImplNode(c)] == true);
                if (selectedCandidate == null) return; // recommended dependency with no selection -> skip

                ImplementationSelection sel;
                if (Selections.GetImplementation(iface) is {} existing)
                {
                    if (existing.ContainsCommand(demand.Requirements.Command ?? Command.NameRun))
                        return; // already processed this iface for this command -> break cycles
                    existing.AddCommand(demand.Requirements, from: CandidateProvider.LookupOriginalImplementation(existing));
                    sel = existing;
                }
                else
                {
                    sel = selectedCandidate.ToSelection(demand.Requirements, allCandidates: role.Candidates);
                    Selections.Implementations.Add(sel);
                }

                foreach (var sub in DemandsFor(sel, demand.Requirements))
                    Visit(sub);
            }
        }

        private static Literal<NodeKey> LitFor(SelectionCandidate candidate)
            => Literal.Of<NodeKey>(new ImplNode(candidate));

        /// <summary>
        /// Checks whether <paramref name="candidate"/> satisfies the command, version and distribution restrictions captured in <paramref name="requirements"/>.
        /// </summary>
        private static bool MatchesRequirements(SelectionCandidate candidate, Requirements requirements)
        {
            if (!string.IsNullOrEmpty(requirements.Command)
             && !candidate.Implementation.ContainsCommand(requirements.Command))
                return false;

            if (requirements.ExtraRestrictions.TryGetValue(requirements.InterfaceUri, out var range)
             && !range.Match(candidate.Version))
                return false;

            if (requirements.Distributions.Count > 0)
            {
                string candidateDist = (candidate.Implementation as ExternalImplementation)?.Distribution ?? Restriction.DistributionZeroInstall;
                if (!requirements.Distributions.Contains(candidateDist)) return false;
            }

            return true;
        }
    }
}
