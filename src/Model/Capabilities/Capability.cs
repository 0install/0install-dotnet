// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// A capability tells the desktop environment what an application can do and in which fashion this can be represented to the user.
/// </summary>
[XmlType("capability", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public abstract partial class Capability : XmlUnknown, ICloneable<Capability>
{
    /// <summary>
    /// Indicates whether this capability can be registered only machine-wide and not per-user on Windows systems.
    /// </summary>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public virtual bool WindowsMachineWideOnly => false;

    /// <summary>
    /// An ID that differentiates this capability from other capabilities of the same type within the feed.
    /// May only contain alphanumeric characters, spaces ( ), dots (.), underscores (_), hyphens (-) and plus signs (+).
    /// </summary>
    /// <remarks>Also serves as a programmatic identifier within the desktop environment. In case of conflicts, the first capability listed with a specific ID will take precedence.</remarks>
    [Description("An ID that differentiates this capability from other capabilities of the same type within the feed. May only contain alphanumeric characters, spaces ( ), dots (.), underscores (_), hyphens (-) and plus signs (+). Also serves as a programmatic identifier within the desktop environment.")]
    [XmlAttribute("id")]
    public required string ID { get; set; }

    /// <summary>
    /// Identifiers from a namespace global to all <see cref="Capability"/>s.
    /// Collisions in this namespace indicate that the concerned <see cref="Capability"/>s are in conflict cannot be registered on a single system at the same time.
    /// </summary>
    /// <remarks>These identifiers are not guaranteed to stay the same between versions. They should not be stored in files but instead always generated on demand.</remarks>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public abstract IEnumerable<string> ConflictIDs { get; }

    #region Normalize
    /// <summary>
    /// Converts legacy elements, sets default values, etc..
    /// </summary>
    /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
    public virtual void Normalize()
        => EnsureAttributeSafeID(ID, "id");
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="Capability"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Capability"/>.</returns>
    public abstract Capability Clone();
    #endregion
}
