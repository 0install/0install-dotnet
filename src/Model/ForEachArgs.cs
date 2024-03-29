// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Expands an environment variable to multiple arguments.
/// The variable specified in <see cref="ItemFrom"/> is split using <see cref="Separator"/> and the <see cref="Arguments"/> are added once for each item.
/// </summary>
[Description("""
Expands an environment variable to multiple arguments.
The variable specified in ItemFrom is split using Separator and the arguments are added once for each item.
""")]
[Serializable, XmlRoot("for-each", Namespace = Feed.XmlNamespace), XmlType("for-each", Namespace = Feed.XmlNamespace)]
[Equatable]
public partial class ForEachArgs : ArgBase
{
    /// <summary>
    /// The name of the environment variable to be expanded.
    /// </summary>
    [Description("""The name of the environment variable to be expanded.""")]
    [XmlAttribute("item-from")]
    public required string ItemFrom { get; set; }

    /// <summary>
    /// Overrides the default separator character (":" on POSIX and ";" on Windows).
    /// </summary>
    [Description(@"Overrides the default separator character ("":"" on POSIX and "";"" on Windows).")]
    [XmlAttribute("separator"), DefaultValue("")]
    public string? Separator { get; set; }

    /// <summary>
    /// A list of command-line arguments to be passed to an executable. "${item}" will be substituted with each for-each value.
    /// </summary>
    [Browsable(false)]
    [XmlElement("arg")]
    [OrderedEquality]
    public List<Arg> Arguments { get; } = [];

    #region Normalize
    /// <inheritdoc/>
    public override void Normalize()
        => EnsureAttribute(ItemFrom, "item-from");
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the for-each instruction in the form "ItemFrom". Not safe for parsing!
    /// </summary>
    public override string ToString() => ItemFrom;
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="ForEachArgs"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="ForEachArgs"/>.</returns>
    private ForEachArgs CloneForEachArgs() => new()
    {
        ItemFrom = ItemFrom,
        Separator = Separator,
        Arguments = {Arguments.CloneElements()}
    };

    /// <summary>
    /// Creates a deep copy of this <see cref="ForEachArgs"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="ForEachArgs"/>.</returns>
    public override ArgBase Clone() => CloneForEachArgs();
    #endregion
}
