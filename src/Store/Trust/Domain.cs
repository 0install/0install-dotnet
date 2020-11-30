// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Xml.Serialization;
using NanoByte.Common;

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// A specific domain with feeds a <see cref="Key"/> is trusted to sign.
    /// </summary>
    [XmlType("domain", Namespace = TrustDB.XmlNamespace)]
    public struct Domain : ICloneable<Domain>, IEquatable<Domain>
    {
        /// <summary>
        /// A valid domain name (not a full <see cref="Uri"/>!).
        /// </summary>
        [XmlAttribute("value")]
        public string? Value { get; set; }

        /// <summary>
        /// Creates a new domain entry.
        /// </summary>
        /// <param name="value">A valid domain name (not a full <see cref="Uri"/>!).</param>
        public Domain(string? value)
            : this()
        {
            Value = value;
        }

        #region Conversion
        /// <inheritdoc/>
        public override string? ToString() => Value;
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Domain"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Domain"/>.</returns>
        public Domain Clone() => new(Value);
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Domain other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public static bool operator ==(Domain left, Domain right) => left.Equals(right);
        public static bool operator !=(Domain left, Domain right) => !left.Equals(right);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            return obj is Domain domain && Equals(domain);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => (Value ?? "").GetHashCode();
        #endregion
    }
}
