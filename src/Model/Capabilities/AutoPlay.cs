// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common.Collections;

namespace ZeroInstall.Model.Capabilities
{
    /// <summary>
    /// An application's ability to handle one or more AutoPlay events.
    /// </summary>
    [Description("An application's ability to handle one or more AutoPlay events.")]
    [Serializable, XmlRoot("auto-play", Namespace = CapabilityList.XmlNamespace), XmlType("auto-play", Namespace = CapabilityList.XmlNamespace)]
    public sealed class AutoPlay : IconCapability, ISingleVerb, IEquatable<AutoPlay>
    {
        /// <inheritdoc/>
        [XmlIgnore]
        public override bool WindowsMachineWideOnly => false;

        /// <summary>
        /// The name of the application as shown in the AutoPlay selection list.
        /// </summary>
        [Description("The name of the application as shown in the AutoPlay selection list.")]
        [XmlAttribute("provider")]
        public string Provider { get; set; }

        /// <summary>
        /// The command to execute when the handler gets called.
        /// </summary>
        [Browsable(false)]
        [XmlElement("verb")]
        public Verb? Verb { get; set; }

        /// <summary>
        /// The IDs of the events this action can handle.
        /// </summary>
        [Browsable(false)]
        [XmlElement("event")]
        public List<AutoPlayEvent> Events { get; } = new List<AutoPlayEvent>();

        /// <inheritdoc/>
        [XmlIgnore]
        public override IEnumerable<string> ConflictIDs => new[] {"autoplay:" + ID};

        #region Conversion
        /// <summary>
        /// Returns the capability in the form "ID". Not safe for parsing!
        /// </summary>
        public override string ToString()
            => ID ?? "";
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override Capability Clone()
        {
            var capability = new AutoPlay {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID, ExplicitOnly = ExplicitOnly, Provider = Provider, Verb = Verb?.Clone()};
            capability.Icons.AddRange(Icons);
            capability.Descriptions.AddRange(Descriptions.CloneElements());
            capability.Events.AddRange(Events);
            return capability;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(AutoPlay other)
            => other != null
            && base.Equals(other)
            && other.Provider == Provider
            && Equals(other.Verb, Verb)
            && Events.SequencedEquals(other.Events);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is AutoPlay play && Equals(play);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(),
                Provider,
                Verb,
                Events.GetSequencedHashCode());
        #endregion
    }
}
