// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Retrieves an implementation by downloading a single file.
/// </summary>
[Description("Retrieves an implementation by downloading a single file.")]
[Serializable, XmlRoot("file", Namespace = Feed.XmlNamespace), XmlType("file", Namespace = Feed.XmlNamespace)]
[Equatable]
public sealed partial class SingleFile : DownloadRetrievalMethod
{
    /// <summary>
    /// The file's target path relative to the implementation root as a Unix-style path.
    /// </summary>
    [Description("The file's target path relative to the implementation root as a Unix-style path.")]
    [XmlAttribute("dest")]
    public required string Destination { get; set; }

    /// <summary>
    /// Set this to <c>true</c> to mark the file as executable.
    /// </summary>
    [Description("Set this to true to mark the file as executable.")]
    [XmlAttribute("executable"), DefaultValue(false)]
    public bool Executable { get; set; }

    #region Normalize
    /// <inheritdoc/>
    public override void Normalize(FeedUri? feedUri = null)
    {
        base.Normalize(feedUri);

        EnsureAttribute(Destination, "dest");
    }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the file in the form "Location (Size) => Destination". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"{Href} ({Size}) => {Destination}";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="SingleFile"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="SingleFile"/>.</returns>
    public override RetrievalMethod Clone() => new SingleFile {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Href = Href, Size = Size, Destination = Destination, Executable = Executable};
    #endregion
}
