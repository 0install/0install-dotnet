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
using System.Collections.Generic;
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
        public override IEnumerable<VersionRangePart> Intersect(VersionRange versions)
        {
            #region Sanity checks
            if (versions == null) throw new ArgumentNullException(nameof(versions));
            #endregion

            if (versions.Parts.Count == 0)
                yield return this;

            foreach (var part in versions.Parts)
            {
                switch (part)
                {
                    case VersionRangePartRange range:
                        var lowerInclusive = (LowerInclusive == null) || (range.LowerInclusive != null && range.LowerInclusive > LowerInclusive) ? range.LowerInclusive : LowerInclusive;
                        var upperExclusive = (UpperExclusive == null) || (range.UpperExclusive != null && range.UpperExclusive < UpperExclusive) ? range.UpperExclusive : UpperExclusive;
                        if (lowerInclusive == null || upperExclusive == null || lowerInclusive < upperExclusive)
                            yield return new VersionRangePartRange(lowerInclusive, upperExclusive);
                        break;

                    case VersionRangePartExact exact:
                        if (Match(exact.Version)) yield return exact;
                        break;

                    case VersionRangePartExclude exclude:
                        if (!Match(exclude.Version)) yield return this;
                        else throw new NotSupportedException($"Unable to intersect {this} with {exclude}.");
                        break;
                }
            }
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
