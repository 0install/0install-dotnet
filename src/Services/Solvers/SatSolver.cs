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

    /// <summary>Discriminated key for SAT variables: an implementation candidate or a machine-group sentinel.</summary>
    private abstract record NodeKey;
    private sealed record ImplNode(SelectionCandidate Candidate) : NodeKey;
    private sealed record GroupNode(string Group) : NodeKey;

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
        private SatProblem<NodeKey> _problem = new();
        private readonly Dictionary<FeedUri, RoleData> _roles = [];
        private readonly HashSet<string> _machineGroups = [];
        private readonly Queue<FeedUri> _encodingQueue = new();

        protected override bool TryFulfill(SolverDemand rootDemand)
        {
            // Reset state: SolverRunBase.Solve() may call us a second time without the architecture restriction to detect arch-related failures.
            Selections = new Selections {InterfaceUri = rootDemand.Requirements.InterfaceUri, Command = rootDemand.Requirements.Command};
            _problem = new SatProblem<NodeKey>();
            _roles.Clear();
            _machineGroups.Clear();
            _encodingQueue.Clear();

            BuildProblem(rootDemand);

            var model = _problem.Solve(decider: Decide);
            if (model == null) return false;

            ExtractSelections(model, rootDemand.Requirements);
            return true;
        }

        private void BuildProblem(SolverDemand rootDemand)
        {
            var rootRole = VisitRole(rootDemand.Requirements);

            // Root: at least one candidate matching the requested command (and version, distribution etc.) must be selected.
            var rootMatching = rootRole.Candidates.Where(c => MatchesRequirements(c, rootDemand.Requirements)).ToList();
            if (rootMatching.Count == 0)
            {
                _problem.AddClause(); // empty clause → unsat
                return;
            }
            _problem.AddClause(rootMatching.Select(LitFor));

            // Encode every candidate's deps + restrictions + commands. VisitRole calls during encoding will append to _encodingQueue.
            while (_encodingQueue.Count > 0)
            {
                var iface = _encodingQueue.Dequeue();
                var role = _roles[iface];
                foreach (var cand in role.Candidates)
                    EncodeCandidateConstraints(cand, role.BaselineRequirements);
            }

            // Cross-role machine-group constraint: e.g. AtMostOne(group32, group64).
            if (_machineGroups.Count >= 2)
                _problem.AtMostOne(_machineGroups.Select(g => Literal.Of<NodeKey>(new GroupNode(g))));
        }

        private RoleData VisitRole(Requirements reqs)
        {
            if (_roles.TryGetValue(reqs.InterfaceUri, out var existing)) return existing;

            // Use a baseline requirements with no command and no dep-specific version restriction so we capture every candidate of the interface that's broadly suitable. Per-dep filtering is applied via clauses, not via the candidate set.
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

            foreach (var c in candidates)
                _problem.AddVariable(new ImplNode(c));

            if (candidates.Count > 1)
                role.AtMostOne = _problem.AtMostOne(candidates.Select(LitFor));

            // Bind each architecture-specific candidate to its machine group.
            foreach (var c in candidates)
            {
                var group = MachineGroupOf(c.Implementation);
                if (group != null)
                {
                    _machineGroups.Add(group);
                    _problem.AddVariable(new GroupNode(group));
                    _problem.Implies(LitFor(c), Literal.Of<NodeKey>(new GroupNode(group)));
                }
            }

            _encodingQueue.Enqueue(reqs.InterfaceUri);
            return role;
        }

        private void EncodeCandidateConstraints(SelectionCandidate cand, Requirements roleReqs)
        {
            var candLit = LitFor(cand);
            var impl = cand.Implementation;

            foreach (var dep in impl.Dependencies)
                if (dep.IsApplicable(roleReqs)) EncodeDependency(candLit, dep);
            foreach (var restriction in impl.Restrictions)
                if (restriction.IsApplicable(roleReqs)) EncodeRestriction(candLit, restriction);
            foreach (var binding in impl.Bindings.OfType<ExecutableInBinding>())
                EncodeSelfBinding(candLit, roleReqs.InterfaceUri, binding);

            // Walk every command of the impl unconditionally. Without command variables, we encode each command's deps as if the command might be needed (a strict over-approximation; tightened by Phase 4 when command variables are added).
            foreach (var cmd in impl.Commands)
            {
                if (cmd.Runner != null) EncodeRunner(candLit, cmd.Runner);
                foreach (var dep in cmd.Dependencies)
                    if (dep.IsApplicable(roleReqs)) EncodeDependency(candLit, dep);
                foreach (var restriction in cmd.Restrictions)
                    if (restriction.IsApplicable(roleReqs)) EncodeRestriction(candLit, restriction);
                foreach (var binding in cmd.Bindings.OfType<ExecutableInBinding>())
                    EncodeSelfBinding(candLit, roleReqs.InterfaceUri, binding);
            }
        }

        private void EncodeDependency(Literal<NodeKey> parent, Dependency dep)
        {
            var depReqs = Require(dep.InterfaceUri, command: "");
            depReqs.Distributions.Add(dep.Distributions);
            if (dep.Versions != null) depReqs.AddRestriction(dep.InterfaceUri, dep.Versions);

            var depRole = VisitRole(depReqs);
            var matching = depRole.Candidates.Where(c => MatchesRequirements(c, depReqs)).ToList();

            if (dep.Importance == Importance.Essential)
            {
                // !parent ∨ matching₁ ∨ matching₂ ∨ ...
                var clause = new List<Literal<NodeKey>>(matching.Count + 1) {parent.Negate()};
                clause.AddRange(matching.Select(LitFor));
                _problem.AddClause(clause);
            }

            // Violator exclusion (applies for both Essential and Recommended deps).
            foreach (var violator in depRole.Candidates.Except(matching))
                _problem.AddClause(parent.Negate(), LitFor(violator).Negate());

            // ExecutableInBindings on the dep require specific commands on the chosen dep candidate.
            foreach (var binding in dep.Bindings.OfType<ExecutableInBinding>())
            {
                if (string.IsNullOrEmpty(binding.Command)) continue;
                if (dep.Importance != Importance.Essential) continue; // recommended-dep command bindings not encoded; over-permissive but safe

                var cmdMatching = matching.Where(c => c.Implementation.ContainsCommand(binding.Command)).ToList();
                var clause = new List<Literal<NodeKey>>(cmdMatching.Count + 1) {parent.Negate()};
                clause.AddRange(cmdMatching.Select(LitFor));
                _problem.AddClause(clause);
            }
        }

        private void EncodeRestriction(Literal<NodeKey> parent, Restriction r)
        {
            var depReqs = Require(r.InterfaceUri, command: "");
            depReqs.Distributions.Add(r.Distributions);
            if (r.Versions != null) depReqs.AddRestriction(r.InterfaceUri, r.Versions);

            var depRole = VisitRole(depReqs);

            // <restricts> doesn't require selection: it only excludes violators when this candidate is selected.
            foreach (var violator in depRole.Candidates.Where(c => !MatchesRequirements(c, depReqs)))
                _problem.AddClause(parent.Negate(), LitFor(violator).Negate());
        }

        private void EncodeRunner(Literal<NodeKey> parent, Runner runner)
        {
            // A <runner> on a command is essentially a Dependency on a specific command of the runner interface.
            var runnerReqs = Require(runner.InterfaceUri, command: runner.Command ?? Command.NameRun);
            runnerReqs.Distributions.Add(runner.Distributions);
            if (runner.Versions != null) runnerReqs.AddRestriction(runner.InterfaceUri, runner.Versions);

            var role = VisitRole(runnerReqs);
            var matching = role.Candidates.Where(c => MatchesRequirements(c, runnerReqs)).ToList();

            var clause = new List<Literal<NodeKey>>(matching.Count + 1) {parent.Negate()};
            clause.AddRange(matching.Select(LitFor));
            _problem.AddClause(clause);

            foreach (var violator in role.Candidates.Except(matching))
                _problem.AddClause(parent.Negate(), LitFor(violator).Negate());
        }

        private void EncodeSelfBinding(Literal<NodeKey> parent, FeedUri iface, ExecutableInBinding binding)
        {
            if (string.IsNullOrEmpty(binding.Command)) return;

            var role = VisitRole(Require(iface, command: ""));
            var matching = role.Candidates.Where(c => c.Implementation.ContainsCommand(binding.Command)).ToList();

            var clause = new List<Literal<NodeKey>>(matching.Count + 1) {parent.Negate()};
            clause.AddRange(matching.Select(LitFor));
            _problem.AddClause(clause);

            foreach (var violator in role.Candidates.Except(matching))
                _problem.AddClause(parent.Negate(), LitFor(violator).Negate());
        }

        private Literal<NodeKey>? Decide()
        {
            var seen = new HashSet<FeedUri>();
            return DecideRole(_rootRequirements.InterfaceUri, seen);
        }

        private Literal<NodeKey>? DecideRole(FeedUri iface, HashSet<FeedUri> seen)
        {
            if (!seen.Add(iface)) return null;
            if (!_roles.TryGetValue(iface, out var role)) return null;

            // Prefer to decide the AtMostOne over this role first: pick the first undecided candidate (best version, since GetSortedCandidates orders by preference).
            if (role.AtMostOne != null)
            {
                var amo = role.AtMostOne.BestUndecided();
                if (amo.HasValue) return amo;
            }
            else if (role.Candidates.Count == 1)
            {
                var only = LitFor(role.Candidates[0]);
                if (_problem.GetValue(only) is null) return only;
            }

            // No undecided here; recurse into the selected candidate's deps to find the next undecided role downstream.
            var selected = role.Candidates.FirstOrDefault(c => _problem.GetValue(LitFor(c)) == true);
            if (selected == null) return null;

            // Walk both essential and recommended deps so the decider biases toward selecting recommended candidates when available (matches BacktrackingSolver behavior).
            foreach (var dep in selected.Implementation.Dependencies)
            {
                if (!dep.IsApplicable(role.BaselineRequirements)) continue;
                var sub = DecideRole(dep.InterfaceUri, seen);
                if (sub.HasValue) return sub;
            }
            foreach (var cmd in selected.Implementation.Commands)
            {
                if (cmd.Runner != null)
                {
                    var sub = DecideRole(cmd.Runner.InterfaceUri, seen);
                    if (sub.HasValue) return sub;
                }
                foreach (var dep in cmd.Dependencies)
                {
                    if (!dep.IsApplicable(role.BaselineRequirements)) continue;
                    var sub = DecideRole(dep.InterfaceUri, seen);
                    if (sub.HasValue) return sub;
                }
            }

            return null;
        }

        private void ExtractSelections(Model<NodeKey> model, Requirements rootRequirements)
        {
            Visit(new SolverDemand(rootRequirements, CandidateProvider));

            void Visit(SolverDemand demand)
            {
                var iface = demand.Requirements.InterfaceUri;
                if (!_roles.TryGetValue(iface, out var role)) return;
                var selectedCandidate = role.Candidates.FirstOrDefault(c => model[new ImplNode(c)] == true);
                if (selectedCandidate == null) return; // recommended dep with no selection -> skip

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

        private static Literal<NodeKey> LitFor(SelectionCandidate c)
            => Literal.Of<NodeKey>(new ImplNode(c));

        /// <summary>Identifies which machine group an implementation belongs to ("32" / "64"), or <c>null</c> for architecture-agnostic implementations.</summary>
        private static string? MachineGroupOf(Implementation impl)
        {
            var cpu = impl.Architecture.Cpu;
            if (cpu.Is64Bit()) return "64";
            if (cpu.Is32Bit()) return "32";
            return null;
        }

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
                string candidateDist = (candidate.Implementation as ExternalImplementation)?.Distribution
                                  ?? Restriction.DistributionZeroInstall;
                if (!requirements.Distributions.Contains(candidateDist)) return false;
            }

            return true;
        }
    }
}
