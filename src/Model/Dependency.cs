// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

#region Enumerations
/// <summary>
/// Describes how important a dependency is (i.e. whether ignoring it is an option).
/// </summary>
public enum Importance
{
    /// <summary>A version of the <see cref="Dependency"/> must be selected.</summary>
    [XmlEnum("essential")]
    Essential,

    /// <summary>No version of the <see cref="Dependency"/> is also an option, although selecting a version is preferable to not selecting one.</summary>
    [XmlEnum("recommended")]
    Recommended
}
#endregion

/// <summary>
/// A reference to an interface that is required as dependency.
/// </summary>
[Description("A reference to an interface that is required as dependency.")]
[Serializable, XmlRoot("requires", Namespace = Feed.XmlNamespace), XmlType("dependency", Namespace = Feed.XmlNamespace)]
[Equatable]
public partial class Dependency : Restriction, IInterfaceUriBindingContainer, ICloneable<Dependency>
{
    /// <summary>
    /// Controls how important this dependency is (i.e. whether ignoring it is an option).
    /// </summary>
    [Description("Controls how important this dependency is (i.e. whether ignoring it is an option).")]
    [XmlAttribute("importance"), DefaultValue(typeof(Importance), "Essential")]
    public Importance Importance { get; set; }

    /// <summary>
    /// This can be used to indicate that this dependency is only needed in some cases. Deprecated; use <see cref="Command"/>s instead.
    /// </summary>
    [Description("This can be used to indicate that this dependency is only needed in some cases; depcreated use <command>s instead.")]
    [XmlAttribute("use"), DefaultValue("")]
    public string? Use { get; set; }

    /// <summary>
    /// A list of <see cref="Binding"/>s for <see cref="Implementation"/>s to locate <see cref="Dependency"/>s.
    /// </summary>
    [Browsable(false)]
    [XmlElement(typeof(GenericBinding)), XmlElement(typeof(EnvironmentBinding)), XmlElement(typeof(OverlayBinding)), XmlElement(typeof(ExecutableInVar)), XmlElement(typeof(ExecutableInPath))]
    [OrderedEquality]
    public List<Binding> Bindings { get; } = [];

    /// <inheritdoc/>
    public override bool IsApplicable(Requirements requirements)
    {
        #region Sanity checks
        if (requirements == null) throw new ArgumentNullException(nameof(requirements));
        #endregion

        return string.IsNullOrEmpty(Use) && base.IsApplicable(requirements);
    }

    #region Normalize
    protected override string XmlTagName => "requires";

    /// <inheritdoc/>
    public override void Normalize()
    {
        base.Normalize();

        // Apply if-0install-version filter
        Bindings.RemoveAll(FilterMismatch);
    }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the dependency in the form "Interface (Use)". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => string.Join(", ", new object?[]
            {
                InterfaceUri,
                Use
            }.Where(x => x is not 0)
             .Select(x => x?.ToString())
             .WhereNotNull());
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="Dependency"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Dependency"/>.</returns>
    Dependency ICloneable<Dependency>.Clone() => new()
    {
        InterfaceUri = InterfaceUri,
        OS = OS,
        Versions = Versions,
        Importance = Importance,
        Use = Use,
        Constraints = {Constraints.CloneElements()},
        Distributions = {Distributions},
        Bindings = {Bindings.CloneElements()}
    };

    /// <summary>
    /// Creates a deep copy of this <see cref="Dependency"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Dependency"/>.</returns>
    public override Restriction Clone() => ((ICloneable<Dependency>)this).Clone();
    #endregion
}
