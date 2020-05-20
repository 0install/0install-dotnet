// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// A known OpenPGP key, trusted to sign feeds from a certain set of domains.
    /// </summary>
    [XmlType("key", Namespace = TrustDB.XmlNamespace)]
    public sealed class Key : ICloneable<Key>, IEquatable<Key>
    {
        /// <summary>
        /// The cryptographic fingerprint of this key.
        /// </summary>
        [XmlAttribute("fingerprint")]
        public string Fingerprint { get; set; }

        /// <summary>
        /// A list of <see cref="Domain"/>s this key is valid for.
        /// </summary>
        [XmlElement("domain")]
        public DomainSet Domains { get; } = new DomainSet();

        #region Conversion
        /// <inheritdoc/>
        public override string ToString() => Fingerprint + " " + Domains;
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Key"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Key"/>.</returns>
        public Key Clone()
        {
            var key = new Key {Fingerprint = Fingerprint};
            key.Domains.AddRange(Domains.CloneElements());
            return key;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Key other)
            => other != null
            && Fingerprint == other.Fingerprint
            && Domains.SetEquals(other.Domains);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is Key key && Equals(key);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(Fingerprint, Domains.GetUnsequencedHashCode());
        #endregion
    }
}
