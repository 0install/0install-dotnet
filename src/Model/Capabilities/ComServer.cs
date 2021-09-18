// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Generator.Equals;

namespace ZeroInstall.Model.Capabilities
{
    /// <summary>
    /// An application's ability to act as a COM server.
    /// </summary>
    [Description("An application's ability to act as a COM server.")]
    [Serializable, XmlRoot("com-server", Namespace = CapabilityList.XmlNamespace), XmlType("com-server", Namespace = CapabilityList.XmlNamespace)]
    [Equatable]
    public sealed partial class ComServer : Capability
    {
        /// <inheritdoc/>
        [Browsable(false), XmlIgnore, IgnoreEquality]
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
    }
}
