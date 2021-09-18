// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Generator.Equals;
using NanoByte.Common.Collections;

namespace ZeroInstall.Model.Capabilities
{
    #region Enumerations
    /// <summary>
    /// Describes how important a dependency is (i.e. whether ignoring it is an option).
    /// </summary>
    public enum ContextMenuTarget
    {
        /// <summary>The context menu entry is displayed for all files.</summary>
        [XmlEnum("files")]
        Files,

        /// <summary>The context menu entry is displayed for executable files.</summary>
        [XmlEnum("executable-files")]
        ExecutableFiles,

        /// <summary>The context menu entry is displayed for all directories.</summary>
        [XmlEnum("directories")]
        Directories,

        /// <summary>The context menu entry is displayed for all filesystem objects (files and directories).</summary>
        [XmlEnum("all")]
        All
    }
    #endregion

    /// <summary>
    /// An entry in the file manager's context menu for all file types.
    /// </summary>
    [Description("An entry in the file manager's context menu for all file types.")]
    [Serializable, XmlRoot("context-menu", Namespace = CapabilityList.XmlNamespace), XmlType("context-menu", Namespace = CapabilityList.XmlNamespace)]
    [Equatable]
    public sealed partial class ContextMenu : VerbCapability
    {
        /// <summary>
        /// Controls which file system object types this context menu entry is displayed for.
        /// </summary>
        [Description("Controls which file system object types this context menu entry is displayed for.")]
        [XmlAttribute("target"), DefaultValue(typeof(ContextMenuTarget), "Files")]
        public ContextMenuTarget Target { get; set; }

        /// <inheritdoc/>
        [Browsable(false), XmlIgnore, IgnoreEquality]
        public override IEnumerable<string> ConflictIDs => Enumerable.Empty<string>();

        #region Conversion
        /// <summary>
        /// Returns the capability in the form "ID". Not safe for parsing!
        /// </summary>
        public override string ToString() => ID;
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override Capability Clone()
        {
            var capability = new ContextMenu {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID, ExplicitOnly = ExplicitOnly, Target = Target};
            capability.Descriptions.AddRange(Descriptions.CloneElements());
            capability.Icons.AddRange(Icons);
            capability.Verbs.AddRange(Verbs.CloneElements());
            return capability;
        }
        #endregion
    }
}
