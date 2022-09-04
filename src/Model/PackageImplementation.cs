// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Design;

namespace ZeroInstall.Model;

/// <summary>
/// An implementation provided by a distribution-specific package manager instead of Zero Install.
/// </summary>
/// <remarks>Any <see cref="Binding"/>s inside <see cref="Dependency"/>s for the <see cref="Feed"/> will be ignored; it is assumed that the requiring component knows how to use the packaged version without further help.</remarks>
[Description("An implementation provided by a distribution-specific package manager instead of Zero Install.")]
[Serializable, XmlRoot("package-implementation", Namespace = Feed.XmlNamespace), XmlType("package-implementation", Namespace = Feed.XmlNamespace)]
[Equatable]
public sealed partial class PackageImplementation : Element
{
    /// <summary>
    /// Well-known values for <see cref="Distributions"/>.
    /// </summary>
    public static readonly string[] DistributionNames = {"Arch", "Cygwin", "Darwin", "Debian", "Gentoo", "MacPorts", "Ports", "RPM", "Slack", "Windows"};

    /// <summary>
    /// The name of the package in the distribution-specific package manager.
    /// </summary>
    [Category("Identity"), Description("The name of the package in the distribution-specific package manager.")]
    [XmlAttribute("package")]
    public string? Package { get; set; }

    // Order is not important (but is preserved), duplicate string entries are not allowed (but not enforced)

    /// <summary>
    /// A list of distribution names (e.g. Debian, RPM) where <see cref="Package"/> applies. Applies everywhere if empty.
    /// </summary>
    [Browsable(false)]
    [XmlIgnore]
    [OrderedEquality]
    public List<string> Distributions { get; } = new();

    /// <summary>
    /// The range of versions to accept for the specified <see cref="Package"/>.
    /// </summary>
    [Category("Release"), Description("The range of versions to accept for the specified Package.")]
    [XmlIgnore]
    public new VersionRange? Version { get; set; }

    #region XML serialization
    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="Distributions"/>
    [Category("Identity"), DisplayName(@"Distributions"), Description("A space-separated list of distribution names (e.g. Debian, RPM) where Package applies. Applies everywhere if empty.")]
    [TypeConverter(typeof(DistributionNameConverter))]
    [XmlAttribute("distributions"), DefaultValue("")]
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

    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="Version"/>
    [XmlAttribute("version"), DefaultValue(""), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public override string? VersionString { get => Version?.ToString(); set => Version = value?.To(x => new VersionRange(x)); }
    #endregion

    #region Normalize
    /// <inheritdoc/>
    protected override void EnsureAttributes()
    {}
    #endregion

    #region Disabled Properties
    /// <summary>Not used.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    [XmlAttribute("version-modifier")]
    public override string? VersionModifier { get => null; set {} }

    /// <summary>Not used.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    [XmlIgnore]
    public override DateTime Released { get => new(); set {} }

    /// <summary>Not used.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    [XmlAttribute("released")]
    public override string? ReleasedString { get => null; set {} }

    /// <summary>Not used.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    [XmlAttribute("stability"), DefaultValue(typeof(Stability), "Unset")]
    public override Stability Stability { get => Stability.Unset; set {} }

    /// <summary>Not used.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
    [XmlAttribute("rollout-percentage"), DefaultValue(0)]
    public override int RolloutPercentage { get => 0; set {} }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the implementation in the form "Package (Distributions)". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"{Package} ({DistributionsString})";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="PackageImplementation"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="PackageImplementation"/>.</returns>
    public PackageImplementation CloneImplementation()
    {
        var implementation = new PackageImplementation
        {
            Package = Package,
            Version = Version,
            Distributions = {Distributions}
        };
        CloneFromTo(this, implementation);
        return implementation;
    }

    /// <summary>
    /// Creates a deep copy of this <see cref="PackageImplementation"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="PackageImplementation"/>.</returns>
    public override Element Clone() => CloneImplementation();
    #endregion
}
