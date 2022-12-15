// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Make a chosen <see cref="Implementation"/> available as an executable path in an environment variable.
/// </summary>
[Description("Make a chosen implementation available as an executable path in an environment variable.")]
[Serializable, XmlRoot("executable-in-var", Namespace = Feed.XmlNamespace), XmlType("executable-in-var", Namespace = Feed.XmlNamespace)]
[Equatable]
public sealed partial class ExecutableInVar : ExecutableInBinding
{
    /// <summary>
    /// The name of the environment variable.
    /// </summary>
    [Description("The name of the environment variable.")]
    [XmlAttribute("name")]
    public required string Name { get; set; }

    #region Normalize
    /// <inheritdoc/>
    public override void Normalize()
        => EnsureAttribute(Name, "name");
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the binding in the form "Name = Command". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"{Name} = {Command ?? Model.Command.NameRun}";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="ExecutableInVar"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="ExecutableInVar"/>.</returns>
    public override Binding Clone() => new ExecutableInVar {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Name = Name, Command = Command};
    #endregion
}
