// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common.Collections;

namespace ZeroInstall.Store.Model.Capabilities
{
    /// <summary>
    /// Abstract base class for capabilities that can have multiple <see cref="Verb"/>s.
    /// </summary>
    [XmlType("verb-capability", Namespace = CapabilityList.XmlNamespace)]
    public abstract class VerbCapability : IconCapability
    {
        /// <summary>
        /// A list of all available operations for the element.
        /// </summary>
        [Browsable(false)]
        [XmlElement("verb")]
        public List<Verb> Verbs { get; } = new List<Verb>();

        #region Equality
        protected bool Equals(VerbCapability other) => other != null && base.Equals(other) && Verbs.SequencedEquals(other.Verbs);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Verbs.GetSequencedHashCode());
        #endregion
    }
}
