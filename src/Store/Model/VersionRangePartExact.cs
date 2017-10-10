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

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// An exact version number like <c>2.0</c> as a part of a <see cref="VersionRange"/>.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    [Serializable]
    public sealed class VersionRangePartExact : VersionRangePart
    {
        public ImplementationVersion Version { get; }

        /// <summary>
        /// Creates a new exact version.
        /// </summary>
        /// <param name="version">The exact version to match.</param>
        public VersionRangePartExact(ImplementationVersion version)
            => Version = version ?? throw new ArgumentNullException(nameof(version));

        /// <inheritdoc/>
        public override VersionRangePart Intersects(Constraint constraint)
        {
            #region Sanity checks
            if (constraint == null) throw new ArgumentNullException(nameof(constraint));
            #endregion

            // If the exact version lies within the constraint, the exact version remains
            if (constraint.NotBefore != null && Version < constraint.NotBefore) return null;
            if (constraint.Before != null && Version >= constraint.Before) return null;
            return this;
        }

        /// <inheritdoc/>
        public override bool Match(ImplementationVersion version) => Version.Equals(version ?? throw new ArgumentNullException(nameof(version)));

        /// <inheritdoc/>
        public override string ToString() => Version.ToString();

        #region Equality
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is VersionRangePartExact exact && Version.Equals(exact.Version);
        }

        public override int GetHashCode() => Version.GetHashCode();
        #endregion
    }
}