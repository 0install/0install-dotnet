// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Make a chosen <see cref="Implementation"/> available as an executable in the search PATH.
/// </summary>
[Description("Make a chosen implementation available as an executable in the search PATH.")]
[Serializable, XmlRoot("executable-in-path", Namespace = Feed.XmlNamespace), XmlType("executable-in-path", Namespace = Feed.XmlNamespace)]
[Equatable]
public sealed partial class ExecutableInPath : ExecutableInBinding
{
    /// <summary>
    /// The name of the executable (without file extensions).
    /// </summary>
    [Description("The name of the executable (without file extensions).")]
    [XmlAttribute("name")]
    public required string Name { get; set; }

    #region Normalize
    /// <inheritdoc/>
    public override void Normalize()
        => EnsureAttribute(Name, "name");
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the binding in the form " Name = Command". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"{Name} = {Command ?? Model.Command.NameRun}";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="ExecutableInPath"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="ExecutableInPath"/>.</returns>
    public override Binding Clone() => new ExecutableInPath {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Name = Name, Command = Command};
    #endregion
}
