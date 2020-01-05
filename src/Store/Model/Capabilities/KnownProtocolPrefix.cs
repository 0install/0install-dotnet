// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common;

namespace ZeroInstall.Store.Model.Capabilities
{
    /// <summary>
    /// Names a well-known protocol prefix. Used for protocols that are shared across many applications (e.g. HTTP, FTP) but not for application-specific protocols.
    /// </summary>
    /// <seealso cref="UrlProtocol.KnownPrefixes"/>
    [Description("Names a well-known protocol prefix. Used for protocols that are shared across many applications (e.g. HTTP, FTP) but not for application-specific protocols.")]
    [Serializable, XmlRoot("known-prefix", Namespace = CapabilityList.XmlNamespace), XmlType("known-prefix", Namespace = CapabilityList.XmlNamespace)]
    public class KnownProtocolPrefix : XmlUnknown, ICloneable<KnownProtocolPrefix>, IEquatable<KnownProtocolPrefix>
    {
        /// <summary>
        /// The value of the prefix (e.g. "http").
        /// </summary>
        [Description("The value of the prefix (e.g. \"http\").")]
        [XmlAttribute("value")]
        public string Value { get; set; }

        #region Conversion
        /// <summary>
        /// Returns the prefix in the form "Value". Not safe for parsing!
        /// </summary>
        public override string ToString()
            => Value ?? "";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="KnownProtocolPrefix"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="KnownProtocolPrefix"/>.</returns>
        public KnownProtocolPrefix Clone() => new KnownProtocolPrefix {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Value = Value};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(KnownProtocolPrefix other) => other != null && base.Equals(other) && other.Value == Value;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is KnownProtocolPrefix prefix && Equals(prefix);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Value);
        #endregion
    }
}
