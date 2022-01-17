// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using System.Xml.Serialization;
using Generator.Equals;
using NanoByte.Common;

namespace ZeroInstall.Model.Preferences;

/// <summary>
/// Stores user-specific preferences for an <see cref="Implementation"/>.
/// </summary>
[XmlType("implementation-preferences", Namespace = Feed.XmlNamespace)]
[Equatable]
public sealed partial class ImplementationPreferences : XmlUnknown, ICloneable<ImplementationPreferences>
{
    /// <summary>
    /// A unique identifier for the implementation. Corresponds to <see cref="ImplementationBase.ID"/>.
    /// </summary>
    [Description("A unique identifier for the implementation.")]
    [XmlAttribute("id")]
    public string ID { get; set; } = default!;

    /// <summary>
    /// A user-specified override for <see cref="Element.Stability"/> specified in the feed.
    /// </summary>
    [Description("A user-specified override for the implementation stability specified in the feed.")]
    [XmlAttribute("user-stability"), DefaultValue(typeof(Stability), "Unset")]
    public Stability UserStability { get; set; } = Stability.Unset;

    /// <summary>
    /// A random number used to compare against <see cref="Element.RolloutPercentage"/>.
    /// </summary>
    [Browsable(false)]
    [XmlAttribute("rollout-percentage"), DefaultValue(0)]
    public int RolloutPercentage { get; set; }

    /// <summary>
    /// Indicates whether this configuration object stores no information other than the <see cref="ID"/> and is thus superfluous.
    /// </summary>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public bool IsSuperfluous
        => UserStability == Stability.Unset
        && RolloutPercentage == 0;

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="ImplementationPreferences"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="ImplementationPreferences"/>.</returns>
    public ImplementationPreferences Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID, UserStability = UserStability};
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the preferences in the form "ImplementationPreferences: ID". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"ImplementationPreferences: {ID}";
    #endregion
}
