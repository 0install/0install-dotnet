// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ZeroInstall.Model.Capabilities
{
    /// <summary>
    /// An application's ability to act as a COM server.
    /// </summary>
    [Description("An application's ability to act as a COM server.")]
    [Serializable, XmlRoot("com-server", Namespace = CapabilityList.XmlNamespace), XmlType("com-server", Namespace = CapabilityList.XmlNamespace)]
    public sealed class ComServer : Capability, IEquatable<ComServer>
    {
        /// <inheritdoc/>
        [XmlIgnore]
        public override IEnumerable<string> ConflictIDs => new[] {"classes:" + ID};

        #region Conversion
        /// <summary>
        /// Returns the capability in the form "-". Not safe for parsing!
        /// </summary>
        public override string ToString() => "-";
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override Capability Clone() => new ComServer {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(ComServer? other) => base.Equals(other);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is ComServer server && Equals(server);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
}
