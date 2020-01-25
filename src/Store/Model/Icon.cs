// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Net;
using ZeroInstall.Store.Model.Design;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// An icon representing the application. Used in the Catalog GUI as well as for desktop icons, menu entries, etc..
    /// </summary>
    /// <seealso cref="Feed.Icons"/>
    /// <seealso cref="EntryPoint.Icons"/>
    [Description("An icon representing the application. Used in the Catalog GUI as well as for desktop icons, menu entries, etc..")]
    [Serializable, XmlRoot("icon", Namespace = Feed.XmlNamespace), XmlType("icon", Namespace = Feed.XmlNamespace)]
    public class Icon : FeedElement, ICloneable<Icon>, IEquatable<Icon>
    {
        #region Constants
        /// <summary>
        /// The <see cref="MimeType"/> value for PNG icons.
        /// </summary>
        public const string MimeTypePng = "image/png";

        /// <summary>
        /// The <see cref="MimeType"/> value for Windows ICO icons.
        /// </summary>
        public const string MimeTypeIco = "image/vnd.microsoft.icon";

        /// <summary>
        /// The <see cref="MimeType"/> value for SVG icons.
        /// </summary>
        public const string MimeTypeSvg = "image/svg";

        /// <summary>
        /// All known <see cref="MimeType"/> values for icons.
        /// </summary>
        public static readonly string[] KnownMimeTypes = {MimeTypePng, MimeTypeIco, MimeTypeSvg};
        #endregion

        /// <summary>
        /// The URL used to locate the icon.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public Uri Href { get; set; }

        #region XML serialization
        /// <summary>Used for XML serialization and PropertyGrid.</summary>
        /// <seealso cref="Href"/>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
        [DisplayName(@"Href"), Description("The URL used to locate the icon.")]
        [XmlAttribute("href"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        public string? HrefString { get => Href?.ToStringRfc(); set => Href = (string.IsNullOrEmpty(value) ? null : new Uri(value, UriKind.Absolute)); }
        #endregion

        /// <summary>
        /// The MIME type of the icon. This value is case-insensitive.
        /// </summary>
        [Description("The MIME type of the icon. This value is case-insensitive.")]
        [TypeConverter(typeof(IconMimeTypeConverter))]
        [XmlAttribute("type"), DefaultValue("")]
        public string? MimeType { get; set; }

        #region Normalize
        /// <summary>
        /// Performs sanity checks.
        /// </summary>
        /// <exception cref="InvalidDataException">One or more required fields are not set.</exception>
        /// <remarks>This method should be called to prepare a <see cref="Feed"/> for solver processing. Do not call it if you plan on serializing the feed again since it may loose some of its structure.</remarks>
        public void Normalize() => EnsureNotNull(Href, xmlAttribute: "href", xmlTag: "icon");
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the icon in the form "Location (MimeType)". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Href} ({MimeType})";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Icon"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Icon"/>.</returns>
        public Icon Clone() => new Icon {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Href = Href, MimeType = MimeType};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Icon other) => other != null && base.Equals(other) && other.Href == Href && other.MimeType == MimeType;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is Icon icon && Equals(icon);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Href, MimeType);
        #endregion
    }
}
