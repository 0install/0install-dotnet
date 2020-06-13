// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common.Native;

namespace ZeroInstall.Model.Capabilities
{
    /// <summary>
    /// Indicates that an application should be listed in the "Set your Default Programs" UI (Windows Vista and later).
    /// </summary>
    /// <remarks>The actual integration information is pulled from the other <see cref="Capability"/>s.</remarks>
    [Description("Indicates that an application should be listed in the \"Set your Default Programs\" UI (Windows Vista and later).")]
    [Serializable, XmlRoot("registration", Namespace = CapabilityList.XmlNamespace), XmlType("registration", Namespace = CapabilityList.XmlNamespace)]
    public sealed class AppRegistration : Capability, IEquatable<AppRegistration>
    {
        /// <inheritdoc/>
        public override bool WindowsMachineWideOnly => !WindowsUtils.IsWindows8;

        /// <summary>
        /// The registry path relative to HKEY_CURRENT_USER or HKEY_LOCAL_MACHINE which should be used to store the application's capability registration information.
        /// </summary>
        [Description("The registry path relative to HKEY_CURRENT_USER or HKEY_LOCAL_MACHINE which should be used to store the application's capability registration information.")]
        [XmlAttribute("capability-reg-path")]
        public string CapabilityRegPath { get; set; }

        /// <inheritdoc/>
        [XmlIgnore]
        public override IEnumerable<string> ConflictIDs => new[] {"registered-apps:" + ID, "hklm:" + CapabilityRegPath};

        #region Conversion
        /// <summary>
        /// Returns the capability in the form "CapabilityRegPath". Not safe for parsing!
        /// </summary>
        public override string ToString() => CapabilityRegPath;
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override Capability Clone() => new AppRegistration {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID, CapabilityRegPath = CapabilityRegPath};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(AppRegistration other)
            => other != null && base.Equals(other) && other.CapabilityRegPath == CapabilityRegPath;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is AppRegistration registration && Equals(registration);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), CapabilityRegPath);
        #endregion
    }
}
