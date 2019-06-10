// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using JetBrains.Annotations;
using NanoByte.Common;

namespace ZeroInstall.Store.Model.Capabilities
{
    /// <summary>
    /// A capability tells the desktop environment what an application can do and in which fashion this can be represented to the user. It does not change the behaviour of existing UI elements.
    /// </summary>
    [XmlType("capability", Namespace = CapabilityList.XmlNamespace)]
    public abstract class Capability : XmlUnknown, ICloneable<Capability>
    {
        /// <summary>
        /// Indicates whether this capability can be registered only machine-wide and not per-user on Windows systems.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public abstract bool WindowsMachineWideOnly { get; }

        /// <summary>
        /// An ID that differentiates this capability from other capabilities of the same type within the feed. Also serves as a programmatic identifier within the desktop environment.
        /// </summary>
        /// <remarks>In case of conflicts, the first capability listed with a specific ID will take precedence.</remarks>
        [Description("An ID that differentiates this capability from other capabilities of the same type within the feed. Also serves as a programmatic identifier within the desktop environment.")]
        [XmlAttribute("id")]
        public string ID { get; set; }

        /// <summary>
        /// Identifiers from a namespace global to all <see cref="Capability"/>s.
        /// Collisions in this namespace indicate that the concerned <see cref="Capability"/>s are in conflict cannot be registered on a single system at the same time.
        /// </summary>
        /// <remarks>These identifiers are not guaranteed to stay the same between versions. They should not be stored in files but instead always generated on demand.</remarks>
        [Browsable(false)]
        [XmlIgnore, NotNull, ItemNotNull]
        public abstract IEnumerable<string> ConflictIDs { get; }

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Capability"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Capability"/>.</returns>
        public abstract Capability Clone();
        #endregion

        #region Equality
        protected bool Equals(Capability other) => other != null && base.Equals(other) && other.ID == ID;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (ID ?? "").GetHashCode();
            }
        }
        #endregion
    }
}
