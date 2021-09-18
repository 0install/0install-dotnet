// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common;
using ZeroInstall.Model.Properties;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Represents a part of a <see cref="ImplementationVersion"/> containing nothing, a <see cref="VersionModifier"/>, a <see cref="DottedList"/> or both.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    [Serializable]
    public readonly struct VersionPart : IEquatable<VersionPart>, IComparable<VersionPart>
    {
        #region Constants
        /// <summary>
        /// A version number with the value -1.
        /// </summary>
        /// <remarks>-1 or "not set" has an even lower value than a set "0".</remarks>
        public static readonly VersionPart Default = new("-1");
        #endregion

        /// <summary>
        /// The modifier part of the version part.
        /// </summary>
        public VersionModifier Modifier { get; }

        /// <summary>
        /// The dotted list part of the version part.
        /// </summary>
        public VersionDottedList DottedList { get; }

        /// <summary>
        /// Creates a new dotted-list from a a string.
        /// </summary>
        /// <param name="value">The string containing the dotted-list.</param>
        public VersionPart(string value)
            : this()
        {
            #region Sanity checks
            if (value == null) throw new ArgumentNullException(nameof(value));
            #endregion

            if (value.StartsWith("pre", out string? trimmed))
                Modifier = VersionModifier.Pre;
            else if (value.StartsWith("rc", out trimmed))
                Modifier = VersionModifier.RC;
            else if (value.StartsWith("post", out trimmed))
                Modifier = VersionModifier.Post;
            else
            {
                trimmed = value;
                Modifier = VersionModifier.None;
            }

            if (!string.IsNullOrEmpty(trimmed)) DottedList = new VersionDottedList(trimmed);
        }

        #region Conversion
        /// <inheritdoc/>
        public override string ToString()
            => Modifier switch
            {
                VersionModifier.None => "",
                VersionModifier.Pre => "pre",
                VersionModifier.RC => "rc",
                VersionModifier.Post => "post",
                _ => throw new InvalidOperationException(Resources.UnknownModifier)
            } + DottedList;
        #endregion

        #region Equatable
        /// <inheritdoc/>
        public bool Equals(VersionPart other)
            => Modifier == other.Modifier
            && DottedList == other.DottedList;

        public static bool operator ==(VersionPart left, VersionPart right) => left.Equals(right);
        public static bool operator !=(VersionPart left, VersionPart right) => !left.Equals(right);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is VersionPart part && Equals(part);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(Modifier, DottedList);
        #endregion

        #region Comparison
        public int CompareTo(VersionPart other)
        {
            int modifierComparison = ((int)Modifier).CompareTo((int)other.Modifier);
            if (modifierComparison != 0) return modifierComparison;

            return DottedList.CompareTo(other.DottedList);
        }

        public static bool operator <(VersionPart left, VersionPart right) => left.CompareTo(right) < 0;
        public static bool operator >(VersionPart left, VersionPart right) => left.CompareTo(right) > 0;
        public static bool operator <=(VersionPart left, VersionPart right) => left.CompareTo(right) <= 0;
        public static bool operator >=(VersionPart left, VersionPart right) => left.CompareTo(right) >= 0;
        #endregion
    }
}
