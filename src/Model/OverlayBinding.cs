// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Make a chosen <see cref="Implementation"/> available by overlaying it onto another part of the file-system.
    /// </summary>
    /// <remarks>This is to support legacy programs which use hard-coded paths.</remarks>
    [Description("Make a chosen Implementation available by overlaying it onto another part of the file-system.")]
    [Serializable, XmlRoot("overlay", Namespace = Feed.XmlNamespace), XmlType("overlay", Namespace = Feed.XmlNamespace)]
    public sealed class OverlayBinding : Binding, IEquatable<OverlayBinding>
    {
        /// <summary>
        /// The relative path of the directory in the implementation to publish. The default is to publish everything.
        /// </summary>
        [Description("The name of the environment variable. The default is to publish everything.")]
        [XmlAttribute("src"), DefaultValue("")]
        public string Source { get; set; }

        /// <summary>
        /// The mount point on which src is to appear in the filesystem. If missing, '/' (on POSIX) or '%systemdrive%' (on Windows) is assumed.
        /// </summary>
        [Description("The mount point on which src is to appear in the filesystem. If missing, '/' (on POSIX) or '%systemdrive%' (on Windows) is assumed.")]
        [XmlAttribute("mount-point"), DefaultValue("")]
        public string MountPoint { get; set; }

        /// <summary>
        /// Creates a new default overlay binding that publishes the entire implementation to the filesystem root.
        /// </summary>
        public OverlayBinding()
        {
            Source = ".";
        }

        #region Conversion
        /// <summary>
        /// Returns the binding in the form "Source => MountPoint". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Source} => {MountPoint}";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="OverlayBinding"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="OverlayBinding"/>.</returns>
        public override Binding Clone() => new OverlayBinding {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Source = Source, MountPoint = MountPoint};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(OverlayBinding? other)
            => other != null && base.Equals(other) && other.Source == Source && other.MountPoint == MountPoint;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is OverlayBinding binding && Equals(binding);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(), Source, MountPoint);
        #endregion
    }
}
