// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Restricts the set of versions from which the injector may choose an <see cref="Implementation"/>.
/// </summary>
[Description("Restricts the set of versions from which the injector may choose an implementation.")]
[Serializable, XmlRoot("constraint", Namespace = Feed.XmlNamespace), XmlType("constraint", Namespace = Feed.XmlNamespace)]
[Equatable]
public partial class Constraint : FeedElement, ICloneable<Constraint>
{
    /// <summary>
    /// This is the lowest-numbered version that can be chosen.
    /// </summary>
    [Description("This is the lowest-numbered version that can be chosen.")]
    [XmlIgnore]
    public ImplementationVersion? NotBefore { get; set; }

    /// <summary>
    /// This version and all later versions are unsuitable.
    /// </summary>
    [Description("This version and all later versions are unsuitable.")]
    [XmlIgnore]
    public ImplementationVersion? Before { get; set; }

    #region XML serialization
    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="NotBefore"/>
    [XmlAttribute("not-before"), DefaultValue(""), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string? NotBeforeString { get => NotBefore?.ToString(); set => NotBefore = value?.To(x => new ImplementationVersion(x)); }

    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="Before"/>
    [XmlAttribute("before"), DefaultValue(""), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string? BeforeString { get => Before?.ToString(); set => Before = value?.To(x => new ImplementationVersion(x)); }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the constraint in the form "NotBefore =&lt; Ver %lt; Before". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"{NotBefore} =< Ver < {Before}";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a copy of this <see cref="Constraint"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Constraint"/>.</returns>
    public Constraint Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, NotBefore = NotBefore, Before = Before};
    #endregion
}
