// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// A single command-line arguments to be passed to an executable.
/// </summary>
[Description("A single command-line arguments to be passed to an executable.")]
[Serializable, XmlRoot("arg", Namespace = Feed.XmlNamespace), XmlType("arg", Namespace = Feed.XmlNamespace)]
[Equatable]
public partial class Arg : ArgBase, ICloneable<Arg>
{
    /// <summary>
    /// A single command-line arguments to be passed to an executable.
    /// Will be automatically escaped to allow proper concatenation of multiple arguments containing spaces.
    /// </summary>
    [Description("""
    A single command-line arguments to be passed to an executable.
    Will be automatically escaped to allow proper concatenation of multiple arguments containing spaces.
    """)]
    [XmlText]
    public required string Value { get; set; }

    #region Normalize
    /// <inheritdoc/>
    public override void Normalize()
        => EnsureAttribute(Value, "value");
    #endregion

    #region Conversion
    /// <summary>
    /// Convenience cast for turning strings into plain <see cref="Arg"/>s.
    /// </summary>
    public static implicit operator Arg(string value) => new() {Value = value};

    /// <summary>
    /// Returns <see cref="Value"/>. Not safe for parsing!
    /// </summary>
    public override string ToString() => Value;
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="Arg"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Arg"/>.</returns>
    Arg ICloneable<Arg>.Clone() => new() {Value = Value};

    /// <summary>
    /// Creates a deep copy of this <see cref="Arg"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Arg"/>.</returns>
    public override ArgBase Clone() => ((ICloneable<Arg>)this).Clone();
    #endregion
}
