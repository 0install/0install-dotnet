// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Design;

namespace ZeroInstall.Model;

/// <summary>
/// Associates a <see cref="Command"/> with a user-friendly name and description.
/// </summary>
/// <seealso cref="Feed.EntryPoints"/>
[Description("Associates a command with a user-friendly name and description.")]
[Serializable, XmlRoot("entry-point", Namespace = Feed.XmlNamespace), XmlType("entry-point", Namespace = Feed.XmlNamespace)]
[Equatable]
public sealed partial class EntryPoint : FeedElement, IIconContainer, ISummaryContainer, ICloneable<EntryPoint>
{
    /// <summary>
    /// The name of the <see cref="Command"/> this entry point represents.
    /// </summary>
    [Description(@"The name of the command this entry point represents.")]
    [TypeConverter(typeof(CommandNameConverter))]
    [XmlAttribute("command")]
    public required string Command { get; set; }

    /// <summary>
    /// The canonical name of the binary supplying the command (without file extensions). This is used to suggest suitable alias names.
    /// </summary>
    /// <remarks>Will default to <see cref="Command"/> when left <c>null</c>.</remarks>
    [Description("""The canonical name of the binary supplying the command (without file extensions). This is used to suggest suitable alias names.""")]
    [XmlAttribute("binary-name"), DefaultValue("")]
    public string? BinaryName { get; set; }

    /// <summary>
    /// The Application User Model ID; used by Windows to associate shortcuts and pinned taskbar entries with running processes.
    /// May not be longer than 128 characters and may not contain whitespace.
    /// </summary>
    [Description("""The Application User Model ID; used by Windows to associate shortcuts and pinned taskbar entries with running processes. May not be longer than 128 characters and may not contain whitespace.""")]
    [XmlAttribute("app-id")]
    public string? AppId { get; set; }

    /// <summary>
    /// If <c>true</c>, indicates that the <see cref="Command"/> represented by this entry point requires a terminal in order to run.
    /// </summary>
    [Description("""If true, indicates that the Command represented by this entry point requires a terminal in order to run.""")]
    [XmlIgnore, DefaultValue(false)]
    public bool NeedsTerminal { get; set; }

    /// <summary>
    /// If <c>true</c>, indicates that this entry point should be offered as an auto-start candidate to the user.
    /// </summary>
    [Description("""If true, indicates that this entry point should be offered as an auto-start candidate to the user.""")]
    [XmlIgnore, DefaultValue(false)]
    public bool SuggestAutoStart { get; set; }

    /// <summary>
    /// If <c>true</c>, indicates that this entry point should be offered as a candidate for the "Send To" context menu to the user.
    /// </summary>
    [Description("""If true, indicates that this entry point should be offered as a candidate for the "Send To" context menu to the user.""")]
    [XmlIgnore, DefaultValue(false)]
    public bool SuggestSendTo { get; set; }

    #region XML serialization
    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="NeedsTerminal"/>
    [XmlElement("needs-terminal"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string? NeedsTerminalString { get => (NeedsTerminal ? "" : null); set => NeedsTerminal = (value != null); }

    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="SuggestAutoStart"/>
    [XmlElement("suggest-auto-start"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string? SuggestAutoStartString { get => (SuggestAutoStart ? "" : null); set => SuggestAutoStart = (value != null); }

    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="SuggestSendTo"/>
    [XmlElement("suggest-send-to"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string? SuggestSendToString { get => (SuggestSendTo ? "" : null); set => SuggestSendTo = (value != null); }
    #endregion

    /// <summary>
    /// User-friendly names for the command. If not present, <see cref="Command"/> is used instead.
    /// </summary>
    [Browsable(false)]
    [XmlElement("name")]
    [OrderedEquality]
    public LocalizableStringCollection Names { get; } = [];

    /// <inheritdoc/>
    [Browsable(false)]
    [XmlElement("summary")]
    [OrderedEquality]
    public LocalizableStringCollection Summaries { get; } = [];

    /// <inheritdoc/>
    [Browsable(false)]
    [XmlElement("description")]
    [OrderedEquality]
    public LocalizableStringCollection Descriptions { get; } = [];

    /// <summary>
    /// Zero or more icons representing the command. Used for desktop icons, menu entries, etc..
    /// </summary>
    [Browsable(false)]
    [XmlElement("icon")]
    [OrderedEquality]
    public List<Icon> Icons { get; } = [];

    #region Normalize
    /// <summary>
    /// Converts legacy elements, sets default values, etc..
    /// </summary>
    /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
    public void Normalize()
    {
        EnsureAttribute(Command, "command");
        if (AppId != null)
        {
            if (AppId.Length > 128) throw new InvalidDataException(string.Format(Resources.InvalidXmlAttributeOnTag, "app-id", ToShortXml()) + " Should not be longer than 128 characters.");
            if (AppId.ContainsWhitespace()) throw new InvalidDataException(string.Format(Resources.InvalidXmlAttributeOnTag, "app-id", ToShortXml()) + " Should not contain whitespace.");
        }
    }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the EntryPoint in the form "Command (BinaryName)". Not safe for parsing!
    /// </summary>
    public override string ToString() => string.IsNullOrEmpty(BinaryName)
        ? Command
        : $"{Command} ({BinaryName})";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="EntryPoint"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="EntryPoint"/>.</returns>
    public EntryPoint Clone() => new()
    {
        UnknownAttributes = UnknownAttributes,
        UnknownElements = UnknownElements,
        IfZeroInstallVersion = IfZeroInstallVersion,
        Command = Command,
        BinaryName = BinaryName,
        NeedsTerminal = NeedsTerminal,
        Names = {Names.CloneElements()},
        Summaries = {Summaries.CloneElements()},
        Descriptions = {Descriptions.CloneElements()},
        Icons = {Icons.CloneElements()}
    };
    #endregion
}
