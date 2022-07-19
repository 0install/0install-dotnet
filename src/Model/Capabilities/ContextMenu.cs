// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

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

    /// <summary>
    /// A list of file extensions this context menu entry is displayed for. Only applicable when <see cref="Target"/> is <see cref="ContextMenuTarget.Files"/>.
    /// The context menu is shown for all file types when this empty.
    /// </summary>
    [Browsable(false)]
    [XmlElement("extension")]
    [OrderedEquality]
    public List<FileTypeExtension> Extensions { get; } = new();

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
    public override Capability Clone() => new ContextMenu
    {
        UnknownAttributes = UnknownAttributes,
        UnknownElements = UnknownElements,
        ID = ID,
        ExplicitOnly = ExplicitOnly,
        Target = Target,
        Descriptions = {Descriptions.CloneElements()},
        Icons = {Icons.CloneElements()},
        Verbs = {Verbs.CloneElements()},
        Extensions = {Extensions.CloneElements()}
    };
    #endregion
}
