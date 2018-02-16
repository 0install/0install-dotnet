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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using NanoByte.Common.Collections;
using ZeroInstall.Services.PackageManagers;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Helper functions for <see cref="ISolver"/> implementations.
    /// </summary>
    public static class SolverUtils
    {
        /// <summary>
        /// Returns a list of <see cref="Requirements"/> that substitute blanks with appropriate default values.
        /// Multiple different <see cref="Requirements"/> represent equally valid but mutually exclusive choices such as 32-bit vs 64-bit processes.
        /// </summary>
        /// <param name="requirements">The baseline <see cref="Requirements"/> to extend.</param>
        /// <returns>1 or more alternative <see cref="Requirements"/> ordered from most to least optimal.</returns>
        [NotNull, ItemNotNull]
        public static IEnumerable<Requirements> GetNormalizedAlternatives([NotNull] this Requirements requirements)
        {
            #region Sanity checks
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));
            #endregion

            var effectiveRequirements = requirements.Clone();
            effectiveRequirements.Command = requirements.Command ?? (requirements.Architecture.Cpu == Cpu.Source ? Command.NameCompile : Command.NameRun);
            effectiveRequirements.Architecture = new Architecture(
                (effectiveRequirements.Architecture.OS == OS.All) ? Architecture.CurrentSystem.OS : effectiveRequirements.Architecture.OS,
                (effectiveRequirements.Architecture.Cpu == Cpu.All) ? Architecture.CurrentSystem.Cpu : effectiveRequirements.Architecture.Cpu);
            if (effectiveRequirements.Languages.Count == 0) effectiveRequirements.Languages.Add(CultureInfo.CurrentUICulture);

            if (effectiveRequirements.Architecture.Cpu == Cpu.X64)
            { // x86-on-X64 compatability
                var x86Requirements = effectiveRequirements.Clone();
                x86Requirements.Architecture = new Architecture(x86Requirements.Architecture.OS, Cpu.I686);
                return new[] {effectiveRequirements, x86Requirements};
            }
            else return new[] {effectiveRequirements};
        }

        /// <summary>
        /// Turns a <see cref="SelectionCandidate"/> into a <see cref="ImplementationSelection"/>.
        /// </summary>
        /// <param name="candidate">The selected candidate.</param>
        /// <param name="allCandidates">All candidates that were considered for selection (including the selected one). These are used to present the user with possible alternatives.</param>
        /// <param name="requirements">The requirements the selected candidate was chosen for.</param>
        [NotNull]
        public static ImplementationSelection ToSelection([NotNull] this SelectionCandidate candidate, [NotNull, ItemNotNull] IEnumerable<SelectionCandidate> allCandidates, [NotNull] Requirements requirements)
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
                InterfaceUri = requirements.InterfaceUri
            };
            if (candidate.FeedUri != requirements.InterfaceUri) selection.FromFeed = candidate.FeedUri;

            if (implementation is ExternalImplementation externalImplementation)
                selection.QuickTestFile = externalImplementation.QuickTestFile;

            selection.Bindings.AddRange(implementation.Bindings.CloneElements());
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
        public static void AddDependencies([NotNull] this IDependencyContainer target, [NotNull] Requirements requirements, [NotNull] IDependencyContainer from)
        {
            #region Sanity checks
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));
            if (from == null) throw new ArgumentNullException(nameof(from));
            #endregion

            target.Dependencies.AddRange(from.Dependencies.Where(x => x.IsApplicable(requirements)).CloneElements());
            target.Restrictions.AddRange(from.Restrictions.Where(x => x.IsApplicable(requirements)).CloneElements());
        }

        /// <summary>
        /// Adds a <see cref="Command"/> specified in an <see cref="Implementation"/> to a <see cref="ImplementationSelection"/>.
        /// </summary>
        /// <param name="selection">The <see cref="ImplementationSelection"/> to add the <see cref="Command"/> to.</param>
        /// <param name="requirements">The requirements specifying which <see cref="Requirements.Command"/> to extract.</param>
        /// <param name="from">The <see cref="Implementation"/> to get the <see cref="Command"/> from.</param>
        /// <returns>The <see cref="Command"/> that was added to <paramref name="selection"/>; <c>null</c> if none.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "This method explicitly transfers information from an Implementation to an ImplementationSelection.")]
        [CanBeNull]
        public static Command AddCommand([NotNull] this ImplementationSelection selection, [NotNull] Requirements requirements, [NotNull] Implementation from)
        {
            #region Sanity checks
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));
            if (from == null) throw new ArgumentNullException(nameof(from));
            #endregion

            Debug.Assert(requirements.Command != null);
            var command = from[requirements.Command];
            if (command == null) return null;

            var newCommand = new Command {Name = command.Name, Path = command.Path};
            newCommand.Arguments.AddRange(command.Arguments.CloneElements());
            newCommand.Bindings.AddRange(command.Bindings.CloneElements());
            if (command.WorkingDir != null) newCommand.WorkingDir = command.WorkingDir.Clone();
            if (command.Runner != null) newCommand.Runner = command.Runner.CloneRunner();
            newCommand.AddDependencies(requirements, from: command);

            selection.Commands.Add(newCommand);
            return newCommand;
        }

        /// <summary>
        /// Adds the version restriction from <paramref name="source"/> to the <paramref name="requirements"/>.
        /// </summary>
        public static void AddRestriction([NotNull] this Requirements requirements, [NotNull] Restriction source)
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
        public static void AddRestrictions([NotNull] this Requirements requirements, [NotNull] Requirements source)
        {
            #region Sanity checks
            if (requirements == null) throw new ArgumentNullException(nameof(requirements));
            if (source == null) throw new ArgumentNullException(nameof(source));
            #endregion

            foreach (var restriction in source.ExtraRestrictions)
                requirements.AddRestriction(restriction.Key, restriction.Value);
        }

        /// <summary>
        /// Checks wether a set of selection candidates contains an implementation with a specific ID.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "This method only operate on Selections.")]
        public static bool Contains([NotNull] this IEnumerable<SelectionCandidate> candidates, [NotNull] ImplementationSelection implementation)
        {
            #region Sanity checks
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            #endregion

            return candidates.Select(x => x.Implementation.ID).Contains(implementation.ID);
        }

        /// <summary>
        /// Removes all <see cref="Restriction"/>s from <see cref="Selections"/>.
        /// </summary>
        public static void PurgeRestrictions([NotNull] this Selections selections)
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
    }
}
