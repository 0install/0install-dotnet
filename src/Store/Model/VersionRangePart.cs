/*
 * Copyright 2010-2017 Bastian Eicher
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
using JetBrains.Annotations;
using NanoByte.Common;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Represents an individual non-disjoint part of a <see cref="VersionRange"/>.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    [Serializable]
    public abstract class VersionRangePart
    {
        /// <summary>
        /// Parses a string into a <see cref="VersionRange"/> part.
        /// </summary>
        /// <exception cref="FormatException"><paramref name="value"/> is not a valid version range string.</exception>
        [NotNull]
        public static VersionRangePart FromString([NotNull] string value)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
            #endregion

            if (value.Contains(".."))
            {
                string start = value.GetLeftPartAtFirstOccurrence("..");
                var startVersion = string.IsNullOrEmpty(start) ? null : new ImplementationVersion(start);

                ImplementationVersion endVersion;
                string end = value.GetRightPartAtFirstOccurrence("..");
                if (string.IsNullOrEmpty(end)) endVersion = null;
                else
                {
                    if (!end.StartsWith("!")) throw new FormatException(string.Format(Resources.VersionRangeEndNotExclusive, end));
                    endVersion = new ImplementationVersion(end.Substring(1));
                }

                return new VersionRangePartRange(startVersion, endVersion);
            }
            else if (value.StartsWith("!"))
            {
                return new VersionRangePartExclude(
                    new ImplementationVersion(value.Substring(1)));
            }
            else
            {
                return new VersionRangePartExact(
                    new ImplementationVersion(value));
            }
        }

        /// <summary>
        /// Intersects a <see cref="Constraint"/> with this range and returns the result as a new range.
        /// </summary>
        [CanBeNull]
        public abstract VersionRangePart Intersects([NotNull] Constraint constraint);

        /// <summary>
        /// Determines whether a specific version lies within this range.
        /// </summary>
        public abstract bool Match([NotNull] ImplementationVersion version);
    }
}
