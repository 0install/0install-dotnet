// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Renames or moves a file or directory. It is an error if the source or destination are outside the implementation.
/// </summary>
[Description("Renames or moves a file or directory. It is an error if the source or destination are outside the implementation.")]
[Serializable, XmlRoot("rename", Namespace = Feed.XmlNamespace), XmlType("rename", Namespace = Feed.XmlNamespace)]
[Equatable]
public sealed partial class RenameStep : FeedElement, IRecipeStep
{
    /// <summary>
    /// The source file or directory relative to the implementation root as a Unix-style path.
    /// </summary>
    [Description("The source file or directory relative to the implementation root as a Unix-style path.")]
    [XmlAttribute("source"), DefaultValue("")]
    public string Source { get; set; } = default!;

    /// <summary>
    /// The destination file or directory relative to the implementation root as a Unix-style path.
    /// </summary>
    [Description("The destination file or directory relative to the implementation root as a Unix-style path.")]
    [XmlAttribute("dest"), DefaultValue("")]
    public string Destination { get; set; } = default!;

    #region Normalize
    /// <inheritdoc/>
    public void Normalize(FeedUri? feedUri = null)
    {
        EnsureAttribute(Source, "source");
        EnsureAttribute(Destination, "dest");
    }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the rename step in the form "Source => Destination". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"{Source} => {Destination}";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="RenameStep"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="RenameStep"/>.</returns>
    public IRecipeStep Clone() => new RenameStep {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Source = Source, Destination = Destination};
    #endregion
}
