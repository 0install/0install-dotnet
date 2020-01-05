// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;
using ZeroInstall.Store.Model;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// Contains a set of <see cref="AccessPoint"/>s to be registered in a desktop environment.
    /// </summary>
    [Serializable, XmlRoot("access-points", Namespace = AppList.XmlNamespace), XmlType("access-points", Namespace = AppList.XmlNamespace)]
    public sealed class AccessPointList : XmlUnknown, ICloneable<AccessPointList>, IEquatable<AccessPointList>
    {
        /// <summary>
        /// A list of <see cref="AccessPoint"/>s.
        /// </summary>
        [Description("A list of access points.")]
        [XmlElement(typeof(AppAlias)), XmlElement(typeof(AutoStart)), XmlElement(typeof(AutoPlay)), XmlElement(typeof(CapabilityRegistration)), XmlElement(typeof(ContextMenu)), XmlElement(typeof(DefaultProgram)), XmlElement(typeof(DesktopIcon)), XmlElement(typeof(FileType)), XmlElement(typeof(MenuEntry)), XmlElement(typeof(SendTo)), XmlElement(typeof(UrlProtocol)), XmlElement(typeof(QuickLaunch)), XmlElement(typeof(MockAccessPoint))]
        // Note: Can not use ICollection<T> interface with XML Serialization
        public List<AccessPoint> Entries { get; } = new List<AccessPoint>();

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="AccessPointList"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="AccessPointList"/>.</returns>
        public AccessPointList Clone()
        {
            var accessPointList = new AccessPointList {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements};
            accessPointList.Entries.AddRange(Entries.CloneElements());

            return accessPointList;
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the access point list in the form "Entry; Entry; ...". Not safe for parsing!
        /// </summary>
        public override string ToString() => StringUtils.Join("; ", Entries.Select(x => x.ToString()));
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(AccessPointList other)
            => other != null && (base.Equals(other) && Entries.SequencedEquals(other.Entries));

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is AccessPointList list && Equals(list);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Entries.GetSequencedHashCode());
        #endregion
    }
}
