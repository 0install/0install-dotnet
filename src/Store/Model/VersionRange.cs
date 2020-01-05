// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Values.Design;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Represents a (possibly disjoint) set of ranges of <see cref="ImplementationVersion"/>s.
    /// </summary>
    /// <remarks>
    /// <para>This class is immutable.</para>
    /// <para>
    ///   Ranges are separated by pipes (|).
    ///   Each range is in the form "START..!END". The range matches versions where START &lt;= VERSION &lt; END.
    ///   The start or end may be omitted. A single version number may be used instead of a range to match only that version,
    ///   or !VERSION to match everything except that version.
    /// </para>
    /// </remarks>
    [TypeConverter(typeof(StringConstructorConverter<VersionRange>))]
    [Serializable]
    public sealed class VersionRange : IEquatable<VersionRange>
    {
        /// <summary>
        /// An "impossible" range matching no versions.
        /// </summary>
        public static readonly VersionRange None = new VersionRange(new VersionRangePartRange(new ImplementationVersion("0"), new ImplementationVersion("0")));

        /// <summary>
        /// The individual ranges.
        /// </summary>
        public IList<VersionRangePart> Parts { get; }

        /// <summary>
        /// Creates an empty version range (matches everything).
        /// </summary>
        public VersionRange()
        {
            Parts = new VersionRangePart[0];
        }

        /// <summary>
        /// Creates a new version range set.
        /// </summary>
        /// <param name="parts">The individual ranges.</param>
        public VersionRange(params VersionRangePart[] parts)
        {
            Parts = (parts ?? throw new ArgumentNullException(nameof(parts))).ToArray();
        }

        /// <summary>
        /// Creates a new version range set from a a string.
        /// </summary>
        /// <param name="value">The string containing the version ranges.</param>
        /// <exception cref="FormatException"><paramref name="value"/> is not a valid version range string.</exception>
        public VersionRange(string value)
            : this(Array.ConvertAll((value ?? throw new ArgumentNullException(nameof(value))).Split('|'), part => VersionRangePart.FromString(part.Trim())))
        {}

        /// <summary>
        /// Convenience cast for <see cref="ImplementationVersion"/>s into <see cref="VersionRange"/>s that match that exact version.
        /// </summary>
        public static implicit operator VersionRange(ImplementationVersion version)
            => (version == null) ? null : new VersionRange(new VersionRangePartExact(version));

        /// <summary>
        /// Convenience cast for <see cref="Constraint"/>s into <see cref="VersionRange"/>s.
        /// </summary>
        public static implicit operator VersionRange(Constraint constraint)
            => (constraint == null) ? null : new VersionRange(new VersionRangePartRange(constraint.NotBefore, constraint.Before));

        /// <summary>
        /// Creates a new <see cref="VersionRange"/> using the specified string representation.
        /// </summary>
        /// <param name="value">The string to parse.</param>
        /// <param name="result">Returns the created <see cref="VersionRange"/> if successfully; <c>null</c> otherwise.</param>
        /// <returns><c>true</c> if the <see cref="VersionRange"/> was successfully created; <c>false</c> otherwise.</returns>
        public static bool TryCreate(
            string value,
#if NETSTANDARD2_1
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]
#endif
            out VersionRange? result)
        {
            try
            {
                result = new VersionRange(value);
                return true;
            }
            catch (ArgumentException)
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Intersects another version range set with this one and returns a new set as the result.
        /// </summary>
        public VersionRange Intersect(VersionRange other)
        {
            #region Sanity checks
            if (other == null) throw new ArgumentNullException(nameof(other));
            #endregion

            if (Parts.Count == 0) return other;

            var parts = Parts.SelectMany(x => x.Intersect(other)).Distinct().ToArray();
            return parts.Length == 0 ? None : new VersionRange(parts);
        }

        /// <summary>
        /// Determines whether a specific version lies within this range set.
        /// </summary>
        public bool Match(ImplementationVersion version)
        {
            #region Sanity checks
            if (version == null) throw new ArgumentNullException(nameof(version));
            #endregion

            if (Parts.Count == 0) return true;
            return Parts.Any(part => part.Match(version));
        }

        /// <summary>
        /// Returns a string representation of the version range set. Safe for parsing!
        /// </summary>
        public override string ToString() => StringUtils.Join("|", Parts.Select(part => part.ToString()));

        #region Equality
        /// <inheritdoc/>
        public bool Equals(VersionRange other)
        {
            // Cancel if the number of parts don't match
            if (Parts.Count != other?.Parts.Count)
                return false;

            // Cancel if one of the parts does not match
            for (int i = 0; i < Parts.Count; i++)
            {
                if (!Parts[i].Equals(other.Parts[i]))
                    return false;
            }

            // If we reach this, everything was equal
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is VersionRange range && Equals(range);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => Parts.GetSequencedHashCode();

        public static bool operator ==(VersionRange? left, VersionRange? right) => Equals(left, right);
        public static bool operator !=(VersionRange? left, VersionRange? right) => !Equals(left, right);
        #endregion
    }
}
