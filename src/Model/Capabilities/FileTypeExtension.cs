// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common;

namespace ZeroInstall.Model.Capabilities
{
    /// <summary>
    /// A specific file extension used to identify a file type.
    /// </summary>
    [Description("A specific file extension used to identify a file type.")]
    [Serializable, XmlRoot("extension", Namespace = CapabilityList.XmlNamespace), XmlType("extension", Namespace = CapabilityList.XmlNamespace)]
    public class FileTypeExtension : XmlUnknown, ICloneable<FileTypeExtension>, IEquatable<FileTypeExtension>
    {
        #region Constants
        /// <summary>
        /// Canonical <see cref="PerceivedType"/>.
        /// </summary>
        public const string TypeFolder = "folder", TypeText = "text", TypeImage = "image", TypeAudio = "audio", TypeVideo = "video", TypeCompressed = "compressed", TypeDocument = "document", TypeSystem = "system", TypeApplication = "application", TypeGameMedia = "gamemedia", TypeContacts = "contacts";
        #endregion

        /// <summary>
        /// The file extension including the leading dot (e.g. ".png").
        /// </summary>
        [Description("The file extension including the leading dot (e.g. \".png\").")]
        [XmlAttribute("value")]
        public string Value { get; set; }

        /// <summary>
        /// The MIME type associated with the file extension.
        /// </summary>
        [Description("The MIME type associated with the file extension.")]
        [XmlAttribute("mime-type"), DefaultValue("")]
        public string MimeType { get; set; }

        /// <summary>
        /// Defines the broad category of file types (e.g. text, image, audio) this extension falls into. Should always be a canonical type.
        /// </summary>
        [Description("Defines the broad category of file (e.g. text, image, audio) types this extension falls into. Should always be a canonical type.")]
        [XmlAttribute("perceived-type"), DefaultValue("")]
        public string PerceivedType { get; set; }

        #region Conversion
        /// <summary>
        /// Returns the extension in the form "Value (MimeType)". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Value} ({MimeType})";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="FileTypeExtension"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="FileTypeExtension"/>.</returns>
        public FileTypeExtension Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Value = Value, MimeType = MimeType, PerceivedType = PerceivedType};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(FileTypeExtension? other)
            => other != null
            && base.Equals(other)
            && other.Value == Value
            && other.MimeType == MimeType
            && other.PerceivedType == PerceivedType;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is FileTypeExtension extension && Equals(extension);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(),
                Value,
                MimeType,
                PerceivedType);
        #endregion
    }
}
