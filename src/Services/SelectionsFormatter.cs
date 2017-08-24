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
using System.Text;
using JetBrains.Annotations;
using ZeroInstall.Services.PackageManagers;
using ZeroInstall.Services.Properties;
using ZeroInstall.Store;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services
{
    /// <summary>
    /// Formats <see cref="Selections"/> for string output.
    /// </summary>
    public static class SelectionsFormatter
    {
        /// <summary>
        /// Generates a human-readable representation of the implementation selection hierachy.
        /// </summary>
        /// <param name="selections">The selections to be displayed.</param>
        /// <param name="store">A store to search for implementation storage locations.</param>
        [NotNull]
        public static string GetHumanReadable([NotNull] this Selections selections, [NotNull] IStore store)
        {
            #region Sanity checks
            if (selections == null) throw new ArgumentNullException(nameof(selections));
            if (store == null) throw new ArgumentNullException(nameof(store));
            #endregion

            var builder = new StringBuilder();
            PrintNode(selections, builder, new HashSet<FeedUri>(), store, "", selections.InterfaceUri);
            return builder.ToString();
        }

        /// <summary>
        /// Helper method for <see cref="GetHumanReadable"/> that recursivley writes information about <see cref="ImplementationSelection"/>s to a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="selections">The selections to be displayed.</param>
        /// <param name="builder">The string builder to write the output to.</param>
        /// <param name="handled">A list of interface URIs that have already been handled; used to prevent infinite recursion.</param>
        /// <param name="store">A store to search for implementation storage locations.</param>
        /// <param name="indent">An indention prefix for the current recursion level (to create a visual hierachy).</param>
        /// <param name="interfaceUri">The <see cref="ImplementationSelection.InterfaceUri"/> to look for.</param>
        private static void PrintNode(Selections selections, StringBuilder builder, HashSet<FeedUri> handled, IStore store, string indent, FeedUri interfaceUri)
        {
            // Prevent infinite recursion
            if (handled.Contains(interfaceUri)) return;
            handled.Add(interfaceUri);

            builder.AppendLine(indent + "- URI: " + interfaceUri);
            try
            {
                var implementation = selections[interfaceUri];
                builder.AppendLine(indent + "  Version: " + implementation.Version);
                builder.AppendLine(indent + "  Path: " + (implementation.LocalPath ?? implementation.GetPath(store) ?? Resources.NotCached));
                builder.AppendLine();

                indent += "    ";

                // Recurse into regular dependencies
                foreach (var dependency in implementation.Dependencies)
                    PrintNode(selections, builder, handled, store, indent, dependency.InterfaceUri);

                if (implementation.Commands.Count != 0)
                {
                    var command = implementation.Commands[0];

                    // Recurse into command dependencies
                    foreach (var dependency in command.Dependencies)
                        PrintNode(selections, builder, handled, store, indent, dependency.InterfaceUri);

                    // Recurse into runner dependency
                    if (command.Runner != null)
                        PrintNode(selections, builder, handled, store, indent, command.Runner.InterfaceUri);
                }
            }
            catch (KeyNotFoundException)
            {
                builder.AppendLine(indent + "  " + Resources.NoSelectedVersion);
            }
        }

        /// <summary>
        /// Tries to locate an implementation in an <see cref="IStore"/>.
        /// </summary>
        /// <param name="implementation">The implementation to locate.</param>
        /// <param name="store">The store to search for the implementation storage location.</param>
        /// <returns>A fully qualified path to the directory containing the implementation, a native package name prefixed with <c>package:</c> or <c>null</c> if the implementation is not cached yet.</returns>
        [CanBeNull]
        private static string GetPath([NotNull] this ImplementationSelection implementation, [NotNull] IStore store)
        {
            return implementation.ID.StartsWith(ExternalImplementation.PackagePrefix)
                ? "(" + implementation.ID + ")"
                : store.GetPath(implementation.ManifestDigest);
        }

        /// <summary>
        /// Generates a human-readable list of differences between two selections.
        /// </summary>
        /// <param name="newSelections">The new selections to compare against.</param>
        /// <param name="oldSelections">The old selections to base the comparison on.</param>
        [NotNull]
        public static string GetHumanReadableDiff([NotNull] this Selections newSelections, [NotNull] Selections oldSelections)
        {
            #region Sanity checks
            if (newSelections == null) throw new ArgumentNullException(nameof(newSelections));
            if (oldSelections == null) throw new ArgumentNullException(nameof(oldSelections));
            #endregion

            var builder = new StringBuilder();

            foreach (var newImplementation in newSelections.Implementations)
            {
                var interfaceUri = newImplementation.InterfaceUri;
                if (!oldSelections.ContainsImplementation(interfaceUri))
                { // Implementation added
                    builder.AppendLine(interfaceUri + ": new -> " + newImplementation.Version);
                }
            }

            foreach (var oldImplementation in oldSelections.Implementations)
            {
                var interfaceUri = oldImplementation.InterfaceUri;

                var newImplementation = newSelections.GetImplementation(interfaceUri);
                if (newImplementation == null)
                { // Implementation removed
                    builder.AppendLine(interfaceUri + ": removed");
                }
                else if (oldImplementation.Version != newImplementation.Version)
                { // Implementation updated
                    builder.AppendLine(interfaceUri + ": " + oldImplementation.Version + " -> " + newImplementation.Version);
                }
            }

            return builder.ToString();
        }
    }
}
