// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using JetBrains.Annotations;
using NanoByte.Common.Collections;

namespace ZeroInstall.Store.Model.Capabilities
{
    /// <summary>
    /// An application's ability to handle a certain URL protocol such as HTTP.
    /// </summary>
    [Description("An application's ability to handle a certain URL protocol such as HTTP.")]
    [Serializable, XmlRoot("url-protocol", Namespace = CapabilityList.XmlNamespace), XmlType("url-protocol", Namespace = CapabilityList.XmlNamespace)]
    public sealed class UrlProtocol : VerbCapability, IEquatable<UrlProtocol>
    {
        /// <inheritdoc/>
        [XmlIgnore]
        public override bool WindowsMachineWideOnly => false;

        /// <summary>
        /// A well-known protocol prefix such as "http". Should be empty and set in <see cref="Capability.ID"/> instead if it is a custom protocol.
        /// </summary>
        [Browsable(false)]
        [XmlElement("known-prefix"), NotNull]
        public List<KnownProtocolPrefix> KnownPrefixes { get; } = new List<KnownProtocolPrefix>();

        /// <inheritdoc/>
        [XmlIgnore]
        public override IEnumerable<string> ConflictIDs => new[] {"progid:" + ID};

        #region Conversion
        /// <summary>
        /// Returns the capability in the form "ID". Not safe for parsing!
        /// </summary>
        public override string ToString() => ID;
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override Capability Clone()
        {
            var capability = new UrlProtocol {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID, ExplicitOnly = ExplicitOnly};
            capability.Icons.AddRange(Icons);
            capability.Descriptions.AddRange(Descriptions.CloneElements());
            capability.Verbs.AddRange(Verbs.CloneElements());
            capability.KnownPrefixes.AddRange(KnownPrefixes);
            return capability;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(UrlProtocol other) => other != null && base.Equals(other) && KnownPrefixes.SequencedEquals(other.KnownPrefixes);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is UrlProtocol protocol && Equals(protocol);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ KnownPrefixes.GetSequencedHashCode();
            }
        }
        #endregion
    }
}
