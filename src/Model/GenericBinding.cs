// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Zero Install will not know how to run a program using generic bindings itself, but it will include them in any selections documents it creates, which can then be executed by your custom code.
/// </summary>
[Description("Zero Install will not know how to run a program using generic bindings itself, but it will include them in any selections documents it creates, which can then be executed by your custom code.")]
[Serializable, XmlRoot("binding", Namespace = Feed.XmlNamespace), XmlType("binding", Namespace = Feed.XmlNamespace)]
[Equatable]
public sealed partial class GenericBinding : ExecutableInBinding
{
    /// <summary>
    /// If your binding needs a path within the selected implementation, it is suggested that the path attribute be used for this. Other attributes and child elements should be namespaced to avoid collisions.
    /// </summary>
    [Description("If your binding needs a path within the selected implementation, it is suggested that the path attribute be used for this. Other attributes and child elements should be namespaced to avoid collisions. ")]
    [XmlAttribute("path")]
    public string? Path { get; set; }

    #region Conversion
    /// <summary>
    /// Returns the binding in the form "Path = Command". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"{Path} = {Command}";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="GenericBinding"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="GenericBinding"/>.</returns>
    public override Binding Clone()
        => new GenericBinding {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Path = Path, Command = Command};
    #endregion
}
