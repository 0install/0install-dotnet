// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Design;

namespace ZeroInstall.Model;

/// <summary>
/// Restricts the versions of an <see cref="Implementation"/> that are allowed without creating a dependency on the implementation if its was not already chosen.
/// </summary>
[Description("Restricts the versions of an Implementation that are allowed without creating a dependency on the implementation if its was not already chosen.")]
[Serializable, XmlRoot("restricts", Namespace = Feed.XmlNamespace), XmlType("restriction", Namespace = Feed.XmlNamespace)]
[Equatable]
public partial class Restriction : FeedElement, IInterfaceUri, ICloneable<Restriction>
{
    /// <summary>
    /// The URI or local path used to identify the interface.
    /// </summary>
    [Description("The URI or local path used to identify the interface.")]
    [XmlIgnore]
    public required FeedUri InterfaceUri { get; set; }

    /// <summary>
    /// Determines for which operating systems this dependency is required.
    /// </summary>
    [Description("Determines for which operating systems this dependency is required.")]
    [XmlAttribute("os")]
#if !MINIMAL
    [DefaultValue(typeof(OS), "All")]
#endif
    public OS OS { get; set; }

    /// <summary>
    /// A more flexible alternative to <see cref="Constraints"/>.
    /// Each range is in the form "START..!END". The range matches versions where START &lt;= VERSION &lt; END. The start or end may be omitted. A single version number may be used instead of a range to match only that version, or !VERSION to match everything except that version.
    /// </summary>
    [Description("""
    A more flexible alternative to Constraints.
    Each range is in the form "START..!END". The range matches versions where START < VERSION < END. The start or end may be omitted. A single version number may be used instead of a range to match only that version, or !VERSION to match everything except that version.
    """)]
    [XmlIgnore]
    public VersionRange? Versions { get; set; }

    #region XML serialization
    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="InterfaceUri"/>
    [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
    [XmlAttribute("interface"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
    public string InterfaceUriString { get => InterfaceUri?.ToStringRfc()!; set => InterfaceUri = new(value); }

    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="Versions"/>
    [XmlAttribute("version"), DefaultValue(""), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string? VersionsString { get => Versions?.ToString(); set => Versions = value?.To(x => new VersionRange(x)); }
    #endregion

    // Order is not important (but is preserved), duplicate entries are not allowed (but not enforced)

    /// <summary>
    /// A list of version <see cref="Constraint"/>s that must be fulfilled.
    /// </summary>
    [Browsable(false)]
    [XmlElement("version")]
    [OrderedEquality]
    public List<Constraint> Constraints { get; } = [];

    // Order is not important (but is preserved), duplicate entries are not allowed (but not enforced)

    /// <summary>
    /// Special value for <see cref="Distributions"/> that requires require an implementation provided by Zero Install (i.e. one not provided by a <see cref="PackageImplementation"/>).
    /// </summary>
    public const string DistributionZeroInstall = "0install";

    /// <summary>
    /// Specifies that the selected implementation must be from one of the given distributions (e.g. Debian, RPM).
    /// The special value <see cref="DistributionZeroInstall"/> may be used to require an implementation provided by Zero Install (i.e. one not provided by a <see cref="PackageImplementation"/>).
    /// </summary>
    /// <seealso cref="KnownDistributions"/>
    [Browsable(false)]
    [XmlIgnore]
    [OrderedEquality]
    public List<string> Distributions { get; } = [];

    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="Distributions"/>
    [DisplayName(@"Distributions"), Description("""
    Specifies that the selected implementation must be from one of the space-separated distributions (e.g. Debian, RPM).
    The special value '0install' may be used to require an implementation provided by Zero Install (i.e. one not provided by a <package-implementation>).
    """)]
    [TypeConverter(typeof(DistributionNameConverter))]
    [XmlAttribute("distribution"), DefaultValue("")]
    [IgnoreEquality]
    public string DistributionsString
    {
        get => string.Join(" ", Distributions);
        set
        {
            Distributions.Clear();
            if (string.IsNullOrEmpty(value)) return;
            Distributions.Add(value.Split(' '));
        }
    }

    /// <summary>
    /// Determines whether this reference is applicable for the given <paramref name="requirements"/>.
    /// </summary>
    public virtual bool IsApplicable(Requirements requirements)
    {
        #region Sanity checks
        if (requirements == null) throw new ArgumentNullException(nameof(requirements));
        #endregion

        return OS.RunsOn(requirements.Architecture.OS);
    }

    #region Normalize
    protected virtual string XmlTagName => "restricts";

    /// <summary>
    /// Flattens inheritance structures, Converts legacy elements, sets default values, etc..
    /// </summary>
    /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
    public virtual void Normalize()
    {
        EnsureAttribute(InterfaceUri, "interface");

        if (Constraints.Count != 0)
        {
            Versions = Constraints.Aggregate(
                seed: Versions ?? new VersionRange(),
                func: (current, constraint) => current.Intersect(constraint));
            Constraints.Clear();
        }
    }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the dependency in the form "Interface". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => $"{InterfaceUri}";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="Restriction"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Restriction"/>.</returns>
    public virtual Restriction Clone() => new()
    {
        InterfaceUri = InterfaceUri,
        OS = OS,
        Versions = Versions,
        Constraints = {Constraints.CloneElements()},
        Distributions = {Distributions}
    };
    #endregion
}
