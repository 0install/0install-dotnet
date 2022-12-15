// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// A specific <see cref="AutoPlay"/> event such as "Audio CD inserted".
/// </summary>
[Description("""A specific AutoPlay event such as "Audio CD inserted".""")]
[Serializable, XmlRoot("event", Namespace = CapabilityList.XmlNamespace), XmlType("event", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public partial class AutoPlayEvent : XmlUnknown, ICloneable<AutoPlayEvent>
{
    #region Constants
    /// <summary>
    /// Canonical <see cref="Name"/>.
    /// </summary>
    public const string NamePlayCDAudio = "PlayCDAudioOnArrival",
        NamePlayDvdAudioO = "PlayDVDAudioOnArrival",
        NamePlayMusicFiles = "PlayMusicFilesOnArrival",
        NamePlayVideoCDMovie = "PlayVideoCDMovieOnArrival",
        NamePlaySuperVideoCDMovie = "PlaySuperVideoCDMovieOnArrival",
        NamePlayDvdMovie = "PlayDVDMovieOnArrival",
        NamePlayBluRay = "PlayBluRayOnArrival",
        NamePlayVideoFiles = "PlayVideoFilesOnArrival",
        NameBurnCD = "HandleCDBurningOnArrival",
        NameBurnDvd = "HandleDVDBurningOnArrival",
        NameBurnBluRay = "HandleBDBurningOnArrival";
    #endregion

    /// <summary>
    /// The name of the event. May only contain alphanumeric characters, spaces ( ), dots (.), underscores (_), hyphens (-) and plus signs (+).
    /// </summary>
    [Description("The name of the event. May only contain alphanumeric characters, spaces ( ), dots (.), underscores (_), hyphens (-) and plus signs (+).")]
    [XmlAttribute("name")]
    public required string Name { get; set; }

    #region Normalize
    /// <summary>
    /// Converts legacy elements, sets default values, etc..
    /// </summary>
    /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
    public void Normalize()
        => EnsureAttributeSafeID(Name, "name");
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the event in the form "Name". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => $"{Name}";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="AutoPlayEvent"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="AutoPlayEvent"/>.</returns>
    public AutoPlayEvent Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name};
    #endregion
}
