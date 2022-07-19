// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// An application's ability to handle one or more AutoPlay events.
/// </summary>
[Description("An application's ability to handle one or more AutoPlay events.")]
[Serializable, XmlRoot("auto-play", Namespace = CapabilityList.XmlNamespace), XmlType("auto-play", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public sealed partial class AutoPlay : IconCapability
{
    /// <summary>
    /// The name of the application as shown in the AutoPlay selection list.
    /// </summary>
    [Description("The name of the application as shown in the AutoPlay selection list.")]
    [XmlAttribute("provider")]
    public string Provider { get; set; } = default!;

    /// <summary>
    /// The command to execute when the handler gets called.
    /// </summary>
    [Browsable(false)]
    [XmlElement("verb")]
    public Verb Verb { get; set; } = default!;

    /// <summary>
    /// The IDs of the events this action can handle.
    /// </summary>
    [Browsable(false)]
    [XmlElement("event")]
    [OrderedEquality]
    public List<AutoPlayEvent> Events { get; } = new();

    /// <inheritdoc/>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public override IEnumerable<string> ConflictIDs => new[] {"auto-play:" + ID};

    #region Normalize
    /// <inheritdoc/>
    public override void Normalize()
    {
        base.Normalize();
        EnsureAttribute(Provider, "provider");
        if (Verb == null) throw new InvalidDataException(string.Format(Resources.MissingXmlTagInsideTag, "<verb>", ToShortXml()));
        Verb.Normalize();
        foreach (var @event in Events) @event.Normalize();
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
    public override Capability Clone() => new AutoPlay
    {
        UnknownAttributes = UnknownAttributes,
        UnknownElements = UnknownElements,
        ID = ID,
        ExplicitOnly = ExplicitOnly,
        Provider = Provider,
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        Verb = Verb?.Clone()!,
        Icons = {Icons.CloneElements()},
        Descriptions = {Descriptions.CloneElements()},
        Events = {Events.CloneElements()}
    };
    #endregion
}
