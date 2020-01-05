// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common;

namespace ZeroInstall.Store.Model.Capabilities
{
    /// <summary>
    /// A specific <see cref="AutoPlay"/> event such as "Audio CD inserted".
    /// </summary>
    [Description("A specific AutoPlay event such as \"Audio CD inserted\".")]
    [Serializable, XmlRoot("event", Namespace = CapabilityList.XmlNamespace), XmlType("event", Namespace = CapabilityList.XmlNamespace)]
    public class AutoPlayEvent : XmlUnknown, ICloneable<AutoPlayEvent>, IEquatable<AutoPlayEvent>
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
        /// The name of the event.
        /// </summary>
        [Description("The name of the event.")]
        [XmlAttribute("name")]
        public string Name { get; set; }

        #region Conversion
        /// <summary>
        /// Returns the event in the form "Name". Not safe for parsing!
        /// </summary>
        public override string ToString()
            => Name ?? "";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="AutoPlayEvent"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="AutoPlayEvent"/>.</returns>
        public AutoPlayEvent Clone() => new AutoPlayEvent {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(AutoPlayEvent other) => other != null && base.Equals(other) && other.Name == Name;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is AutoPlayEvent @event && Equals(@event);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Name);
        #endregion
    }
}
