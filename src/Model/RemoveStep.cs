// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Removes or moves a file or directory. It is an error if the path is outside the implementation.
/// </summary>
[Description("Removes or moves a file or directory. It is an error if the path is outside the implementation.")]
[Serializable, XmlRoot("remove", Namespace = Feed.XmlNamespace), XmlType("remove", Namespace = Feed.XmlNamespace)]
[Equatable]
public sealed partial class RemoveStep : FeedElement, IRecipeStep
{
    /// <summary>
    /// The file or directory to be removed relative to the implementation root as a Unix-style path.
    /// </summary>
    [Description("The file or directory to be removed relative to the implementation root as a Unix-style path.")]
    [XmlAttribute("path"), DefaultValue("")]
    public string Path { get; set; } = default!;

    #region Normalize
    /// <inheritdoc/>
    public void Normalize(FeedUri? feedUri = null)
        => EnsureAttribute(Path, "path");
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the remove step in the form "Path". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => Path;
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="RemoveStep"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="RemoveStep"/>.</returns>
    public IRecipeStep Clone() => new RemoveStep {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Path = Path};
    #endregion
}
