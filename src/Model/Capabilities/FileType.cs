// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// An application's ability to open a certain file type.
/// </summary>
[Description("An application's ability to open a certain file type.")]
[Serializable, XmlRoot("file-type", Namespace = CapabilityList.XmlNamespace), XmlType("file-type", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public sealed partial class FileType : VerbCapability
{
    /// <summary>
    /// A list of all file extensions associated with this file type.
    /// </summary>
    [Browsable(false)]
    [XmlElement("extension")]
    [OrderedEquality]
    public List<FileTypeExtension> Extensions { get; } = [];

    /// <inheritdoc/>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public override IEnumerable<string> ConflictIDs => [$"progid:{ID}"];

    #region Normalize
    /// <inheritdoc/>
    public override void Normalize()
    {
        base.Normalize();
        foreach (var extension in Extensions) extension.Normalize();
    }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the capability in the form "ID". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => $"{ID}";
    #endregion

    #region Clone
    /// <inheritdoc/>
    public override Capability Clone() => new FileType
    {
        UnknownAttributes = UnknownAttributes,
        UnknownElements = UnknownElements,
        ID = ID,
        ExplicitOnly = ExplicitOnly,
        Descriptions = {Descriptions.CloneElements()},
        Icons = {Icons.CloneElements()},
        Verbs = {Verbs.CloneElements()},
        Extensions = {Extensions.CloneElements()}
    };
    #endregion
}
