// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// Makes an application the default handler for something.
    /// </summary>
    /// <seealso cref="Model.Capabilities.Capability"/>
    [XmlType("default-access-point", Namespace = AppList.XmlNamespace)]
    public abstract class DefaultAccessPoint : AccessPoint
    {
        #region Constants
        /// <summary>
        /// The name of this category of <see cref="AccessPoint"/>s as used by command-line interfaces.
        /// </summary>
        public const string CategoryName = "default-app";
        #endregion

        /// <summary>
        /// The ID of the <see cref="Capability"/> to be made the default handler.
        /// </summary>
        [Description("The ID of the Capability to be made the default handler.")]
        [XmlAttribute("capability")]
        public string Capability { get; set; }

        #region Equality
        protected bool Equals(DefaultAccessPoint other)
            => other != null && base.Equals(other) && other.Capability == Capability;

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Capability);
        #endregion
    }
}
