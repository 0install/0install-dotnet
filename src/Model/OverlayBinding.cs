// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Generator.Equals;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Make a chosen <see cref="Implementation"/> available by overlaying it onto another part of the file-system.
    /// </summary>
    /// <remarks>This is to support legacy programs which use hard-coded paths.</remarks>
    [Description("Make a chosen Implementation available by overlaying it onto another part of the file-system.")]
    [Serializable, XmlRoot("overlay", Namespace = Feed.XmlNamespace), XmlType("overlay", Namespace = Feed.XmlNamespace)]
    [Equatable]
    public sealed partial class OverlayBinding : Binding
    {
        /// <summary>
        /// The relative path of the directory in the implementation to publish. The default is to publish everything.
        /// </summary>
        [Description("The name of the environment variable. The default is to publish everything.")]
        [XmlAttribute("src"), DefaultValue("")]
        public string? Source { get; set; }

        /// <summary>
        /// The mount point on which src is to appear in the filesystem. If missing, '/' (on POSIX) or '%systemdrive%' (on Windows) is assumed.
        /// </summary>
        [Description("The mount point on which src is to appear in the filesystem. If missing, '/' (on POSIX) or '%systemdrive%' (on Windows) is assumed.")]
        [XmlAttribute("mount-point"), DefaultValue("")]
        public string? MountPoint { get; set; }

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
    }
}
