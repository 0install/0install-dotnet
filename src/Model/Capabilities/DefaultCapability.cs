// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using System.Xml.Serialization;
using Generator.Equals;

namespace ZeroInstall.Model.Capabilities
{
    /// <summary>
    /// Abstract base class for capabilities that can be applied as default handlers for something at the user's request.
    /// </summary>
    [XmlType("default-capability", Namespace = CapabilityList.XmlNamespace)]
    [Equatable]
    public abstract partial class DefaultCapability : Capability
    {
        /// <summary>
        /// When set to <c>true</c> this capability is not applied as a default handler without explicit confirmation from the user.
        /// </summary>
        /// <remarks>Use this to exclude exotic capabilities from default integration categories.</remarks>
        [Description("When set to true do not apply this capability is not applied as a default handler without explicit confirmation from the user. Use this to exclude exotic capabilities from default integration categories.")]
        [XmlAttribute("explicit-only"), DefaultValue(false)]
        public bool ExplicitOnly { get; set; }
    }
}
