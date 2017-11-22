/*
 * Copyright 2010-2016 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Dispatch;
using NanoByte.Common.Tasks;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.PackageManagers;
using ZeroInstall.Services.Properties;
using ZeroInstall.Store;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Uses limited backtracking to solve <see cref="Requirements"/>. Does not find all possible solutions!
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public class BacktrackingSolver : ISolver
    {
        #region Dependencies
        private readonly Config _config;
        private readonly IFeedManager _feedManager;
        private readonly IStore _store;
        private readonly IPackageManager _packageManager;
        private readonly ITaskHandler _handler;

        /// <summary>
        /// Creates a new simple solver.
        /// </summary>
        /// <param name="config">User settings controlling network behaviour, solving, etc.</param>
        /// <param name="store">Used to check which <see cref="Implementation"/>s are already cached.</param>
        /// <param name="feedManager">Provides access to remote and local <see cref="Feed"/>s. Handles downloading, signature verification and caching.</param>
        /// <param name="packageManager">An external package manager that can install <see cref="PackageImplementation"/>s.</param>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        public BacktrackingSolver([NotNull] Config config, [NotNull] IFeedManager feedManager, [NotNull] IStore store, [NotNull] IPackageManager packageManager, [NotNull] ITaskHandler handler)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _packageManager = packageManager ?? throw new ArgumentNullException(nameof(packageManager));
            _feedManager = feedManager ?? throw new ArgumentNullException(nameof(feedManager));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }
        #endregion

        /// <inheritdoc/>
        public Selections Solve(Requirements requirements)
        {
            #region Sanity checks
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));
            if (requirements.InterfaceUri == null) throw new ArgumentException(Resources.MissingInterfaceUri, nameof(requirements));
            #endregion

            Log.Info("Running Backtracking Solver for: " + requirements);

            var candidateProvider = new SelectionCandidateProvider(_config, _feedManager, _store, _packageManager, requirements.Languages);
            var selections = requirements
                .GetNormalizedAlternatives()
                .Select(req => new State(req, _handler.CancellationToken, candidateProvider).Solve())
                .WhereNotNull()
                .FirstOrDefault();
            if (selections == null) throw new SolverException("No solution found");
            return selections;
        }

        private class State
        {
            private readonly CancellationToken _cancellationToken;
            private readonly SelectionCandidateProvider _candidateProvider;
            private readonly Requirements _topLevelRequirements;
            private Selections _selections;

            public State([NotNull] Requirements requirements, CancellationToken cancellationToken, [NotNull] SelectionCandidateProvider candidateProvider)
            {
                _cancellationToken = cancellationToken;
                _candidateProvider = candidateProvider;
                _topLevelRequirements = requirements ?? throw new ArgumentNullException(nameof(requirements));
                _selections = new Selections
                {
                    InterfaceUri = requirements.InterfaceUri,
                    Command = requirements.Command
                };
            }

            /// <summary>
            /// Attempts to satisfy the <see cref="_topLevelRequirements"/>.
            /// </summary>
            /// <returns>The resulting <see cref="Selections"/> if a solution was found; <c>null</c> otherwise.</returns>
            [CanBeNull]
            public Selections Solve()
            {
                if (!TryToSolve(_topLevelRequirements)) return null;
                _selections.PurgeRestrictions();
                _selections.Implementations.Sort();
                return _selections;
            }

            private bool TryToSolve([NotNull, ItemNotNull] IEnumerable<Requirements> requirementsSet)
            {
                var required = new List<Requirements>();
                var optional = new List<Requirements>();
                requirementsSet.Bucketize(x => x.Optional).Add(true, optional).Add(false, required).Run();

                var backtrackingSnapshot = _selections.Clone();
                foreach (var premutation in required.Permutate())
                {
                    if (premutation.All(TryToSolve))
                    {
                        optional.ForEach(x => TryToSolve(x));
                        return true;
                    }
                    else _selections = backtrackingSnapshot.Clone();
                }
                return false;
            }

            private bool TryToSolve([NotNull] Requirements requirements)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var allCandidates = _candidateProvider.GetSortedCandidates(requirements);
                var suitableCandidates = allCandidates.Where(candidate =>
                    candidate.IsSuitable && IsCompatibleWithSelections(candidate, requirements.InterfaceUri));

                var existingSelection = _selections.GetImplementation(requirements.InterfaceUri);
                return (existingSelection == null)
                    ? TryToSelectCandidate(suitableCandidates, requirements, allCandidates)
                    : TryToUseExistingSelection(requirements, suitableCandidates, existingSelection);
            }

            private bool IsCompatibleWithSelections([NotNull] SelectionCandidate candidate, [NotNull] FeedUri interfaceUri)
            {
                var nativeImplementation = candidate.Implementation as ExternalImplementation;

                // Ensure the candidate does not conflict with restricions of existing selections
                foreach (var restriction in _selections.RestrictionsFor(interfaceUri))
                {
                    if (restriction.Versions != null && !restriction.Versions.Match(candidate.Version)) return false;
                    if (nativeImplementation != null && !restriction.Distributions.ContainsOrEmpty(nativeImplementation.Distribution)) return false;
                }

                // Ensure the existing selections do not conflict with restrictions of the candidate
                foreach (var restriction in candidate.Implementation.EffectiveRestrictions)
                {
                    var existingSelection = _selections.GetImplementation(restriction.InterfaceUri);
                    if (existingSelection != null)
                    {
                        if (restriction.Versions != null && !restriction.Versions.Match(existingSelection.Version)) return false;
                        if (nativeImplementation != null && !restriction.Distributions.ContainsOrEmpty(nativeImplementation.Distribution)) return false;
                    }
                }

                return true;
            }

            private bool TryToSelectCandidate([NotNull, ItemNotNull] IEnumerable<SelectionCandidate> candidates, [NotNull] Requirements requirements, [NotNull, ItemNotNull] IList<SelectionCandidate> allCandidates)
            {
                foreach (var selection in candidates.Select(x => x.ToSelection(allCandidates, requirements)))
                {
                    var requirementsSet = RequirementsFor(selection, requirements.InterfaceUri, requirements.Command);

                    _selections.Implementations.Add(selection);
                    if (TryToSolve(requirementsSet)) return true;
                    else
                    {
                        _selections.Implementations.RemoveLast();
                        return false;
                    }
                }
                return false;
            }

            private bool TryToUseExistingSelection([NotNull] Requirements requirements, [NotNull, ItemNotNull] IEnumerable<SelectionCandidate> suitableCandidates, [NotNull] ImplementationSelection selection)
            {
                if (!suitableCandidates.Contains(selection)) return false;

                if (selection.ContainsCommand(requirements.Command)) return true;
                else
                {
                    // Add additional command to selection if needed
                    var command = selection.AddCommand(requirements, from: _candidateProvider.LookupOriginalImplementation(selection));
                    return (command == null) || TryToSolve(RequirementsFor(command, requirements.InterfaceUri));
                }
            }

            [NotNull, ItemNotNull]
            private IEnumerable<Requirements> RequirementsFor([NotNull] ImplementationSelection selection, [NotNull] FeedUri interfaceUri, [CanBeNull] string commandName)
            {
                foreach (var dependency in selection.Dependencies)
                {
                    foreach (var requirements in RequirementsFor(dependency))
                        yield return requirements;
                }

                foreach (var requirements in selection.ToBindingRequirements(selection.InterfaceUri))
                    yield return requirements;

                var command = selection[commandName];
                if (command != null)
                {
                    foreach (var requirements in RequirementsFor(command, interfaceUri))
                        yield return requirements;
                }
            }

            [NotNull, ItemNotNull]
            private IEnumerable<Requirements> RequirementsFor([NotNull] Dependency dependency)
            {
                Requirements Mark(Requirements requirements)
                {
                    requirements.Optional = (dependency.Importance != Importance.Essential);
                    return requirements;
                }

                yield return Mark(dependency.ToRequirements(_topLevelRequirements));

                foreach (var requirements in dependency.ToBindingRequirements(dependency.InterfaceUri))
                    yield return Mark(requirements);
            }

            [NotNull, ItemNotNull]
            private IEnumerable<Requirements> RequirementsFor([NotNull] Command command, [NotNull] FeedUri interfaceUri)
            {
                if (command.Bindings.OfType<ExecutableInBinding>().Any())
                    throw new NotSupportedException("<executable-in-*> not supported in <command>");

                if (command.Runner != null)
                    yield return command.Runner.ToRequirements(_topLevelRequirements);

                foreach (var dependency in command.Dependencies)
                {
                    foreach (var requirements in RequirementsFor(dependency))
                        yield return requirements;
                }

                foreach (var requirements in command.ToBindingRequirements(interfaceUri))
                    yield return requirements;
            }
        }
    }
}
