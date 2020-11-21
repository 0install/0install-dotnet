// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NanoByte.Common.Collections;
using ZeroInstall.Model.Properties;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Represents a dotted-list part of a <see cref="ImplementationVersion"/>.
    /// </summary>
    /// <remarks>
    /// This is the syntax for valid dot-separated decimals:
    /// <code>
    /// DottedList := (Integer ("." Integer)*)
    /// </code>
    /// </remarks>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    [Serializable]
    public readonly struct VersionDottedList : IEquatable<VersionDottedList>, IComparable<VersionDottedList>
    {
        /// <summary>
        /// The individual decimals.
        /// </summary>
        public IReadOnlyList<long>? Decimals { get; }

        /// <summary>
        /// Creates a new dotted-list from a a string.
        /// </summary>
        /// <param name="value">The string containing the dotted-list.</param>
        /// <exception cref="FormatException"><paramref name="value"/> is not a valid dotted-list.</exception>
        public VersionDottedList(string value)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
            #endregion

            var parts = value.Split('.');
            var decimals = new long[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (!long.TryParse(parts[i], out decimals[i]))
                    throw new FormatException(Resources.MustBeDottedList);
            }

            Decimals = decimals;
        }

        private static readonly Regex _dottedListPattern = new Regex(@"^(\d+(\.\d+)*)$");

        /// <summary>
        /// Checks whether a string represents a valid dotted-list.
        /// </summary>
        public static bool IsValid(string value) => _dottedListPattern.IsMatch(value);

        #region Conversion
        /// <inheritdoc/>
        public override string ToString()
        {
            if (Decimals == null) return "";

            var output = new StringBuilder();
            for (int i = 0; i < Decimals.Count; i++)
            {
                output.Append(Decimals[i]);

                // Separate parts with dots, no trailing dot
                if (i < Decimals.Count - 1) output.Append(".");
            }

            return output.ToString();
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(VersionDottedList other)
        {
            if (Decimals == null || other.Decimals == null)
                return (Decimals == other.Decimals);

            return Decimals.SequencedEquals(other.Decimals);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => !ReferenceEquals(null, obj) && obj is VersionDottedList list && Equals(list);

        public static bool operator ==(VersionDottedList left, VersionDottedList right) => left.Equals(right);
        public static bool operator !=(VersionDottedList left, VersionDottedList right) => !left.Equals(right);

        /// <inheritdoc/>
        public override int GetHashCode()
            => Decimals?.GetSequencedHashCode() ?? 0;
        #endregion

        #region Comparison
        /// <inheritdoc/>
        public int CompareTo(VersionDottedList other)
        {
            var leftArray = Decimals ?? new long[0];
            var rightArray = other.Decimals ?? new long[0];

            int upperBound = Math.Max(leftArray.Count, rightArray.Count);
            for (int i = 0; i < upperBound; ++i)
            {
                long left = i >= leftArray.Count ? -1 : leftArray[i];
                long right = i >= rightArray.Count ? -1 : rightArray[i];
                int comparisonResult = left.CompareTo(right);
                if (comparisonResult != 0) return left.CompareTo(right);
            }
            return 0;
        }

        public static bool operator <(VersionDottedList left, VersionDottedList right) => left.CompareTo(right) < 0;
        public static bool operator >(VersionDottedList left, VersionDottedList right) => left.CompareTo(right) > 0;
        public static bool operator <=(VersionDottedList left, VersionDottedList right) => left.CompareTo(right) <= 0;
        public static bool operator >=(VersionDottedList left, VersionDottedList right) => left.CompareTo(right) >= 0;
        #endregion
    }
}
