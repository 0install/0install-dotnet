// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Generator.Equals;
using NanoByte.Common;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Switches the working directory of a process on startup to a location within an implementation.
    /// Useful for supporting legacy Windows applications which do not properly locate their installation directory.
    /// </summary>
    /// <seealso cref="Command.WorkingDir"/>
    [Description("Switches the working directory of a process on startup to a location within an implementation.\r\nUseful for supporting legacy Windows applications which do not properly locate their installation directory.")]
    [Serializable, XmlRoot("working-dir", Namespace = Feed.XmlNamespace), XmlType("working-dir", Namespace = Feed.XmlNamespace)]
    [Equatable]
    public sealed partial class WorkingDir : FeedElement, ICloneable<WorkingDir>
    {
        /// <summary>
        /// The relative path of the directory in the implementation to set as the working directory. Defaults to use the root of the implementation if unset.
        /// </summary>
        [Description("The relative path of the directory in the implementation to set as the working directory. Defaults to use the root of the implementation if unset.")]
        [XmlAttribute("src"), DefaultValue("")]
        public string? Source { get; set; }

        #region Conversion
        /// <summary>
        /// Returns the binding in the form "Source". Not safe for parsing!
        /// </summary>
        public override string ToString() => Source ?? "(unset)";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="WorkingDir"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="WorkingDir"/>.</returns>
        public WorkingDir Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Source = Source};
        #endregion
    }
}
