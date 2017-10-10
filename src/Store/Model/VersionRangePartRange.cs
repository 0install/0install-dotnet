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

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// A version range like <c>1.0..!2.0</c> as a part of a <see cref="VersionRange"/>.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    [Serializable]
    public sealed class VersionRangePartRange : VersionRangePart
    {
        [CanBeNull]
        public ImplementationVersion LowerInclusive { get; }

        [CanBeNull]
        public ImplementationVersion UpperExclusive { get; }

        /// <summary>
        /// Creates a new version range.
        /// </summary>
        /// <param name="lowerInclusive">The lower inclusive bound. May be <c>null</c>.</param>
        /// <param name="upperExclusive">The upper exclusive bound. May be <c>null</c>.</param>
        public VersionRangePartRange([CanBeNull] ImplementationVersion lowerInclusive, [CanBeNull] ImplementationVersion upperExclusive)
        {
            LowerInclusive = lowerInclusive;
            UpperExclusive = upperExclusive;
        }

        /// <inheritdoc/>
        public override VersionRangePart Intersects(Constraint constraint)
        {
            #region Sanity checks
            if (constraint == null) throw new ArgumentNullException(nameof(constraint));
            #endregion

            // Keep the highest lower bound
            ImplementationVersion startVersion;
            if (LowerInclusive == null || (constraint.NotBefore != null && constraint.NotBefore > LowerInclusive)) startVersion = constraint.NotBefore;
            else startVersion = LowerInclusive;

            // Keep the lowest upper bound
            ImplementationVersion endVersion;
            if (UpperExclusive == null || (constraint.Before != null && constraint.Before < UpperExclusive)) endVersion = constraint.Before;
            else endVersion = UpperExclusive;

            // Exclude impossible ranges
            if (startVersion != null && endVersion != null && startVersion >= endVersion) return null;
            return new VersionRangePartRange(startVersion, endVersion);
        }

        /// <inheritdoc/>
        public override bool Match(ImplementationVersion version)
        {
            #region Sanity checks
            if (version == null) throw new ArgumentNullException(nameof(version));
            #endregion

            if (LowerInclusive != null && version < LowerInclusive) return false;
            if (UpperExclusive != null && version >= UpperExclusive) return false;
            return true;
        }

        /// <inheritdoc/>
        public override string ToString() => LowerInclusive + ".." + (UpperExclusive == null ? "" : ("!" + UpperExclusive));

        #region Equality
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            if (!(obj is VersionRangePartRange other)) return false;
            return Equals(LowerInclusive, other.LowerInclusive) && Equals(UpperExclusive, other.UpperExclusive);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((LowerInclusive?.GetHashCode() ?? 0) * 397) ^ (UpperExclusive?.GetHashCode() ?? 0);
            }
        }
        #endregion
    }
}