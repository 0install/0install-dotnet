// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Net;
using ZeroInstall.Model.Design;

namespace ZeroInstall.Model
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
        public Uri Href { get; set; } = default!;

        #region XML serialization
        /// <summary>Used for XML serialization and PropertyGrid.</summary>
        /// <seealso cref="Href"/>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
        [DisplayName(@"Href"), Description("The URL used to locate the icon.")]
        [XmlAttribute("href"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        // ReSharper disable once ConstantConditionalAccessQualifier
        public string HrefString { get => Href?.ToStringRfc()!; set => Href = new(value, UriKind.Absolute); }
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
        /// Converts legacy elements, sets default values and ensures required elements.
        /// </summary>
        /// <exception cref="InvalidDataException">One or more required elements are not set.</exception>
        public void Normalize()
            => EnsureAttribute(Href, "href");
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
        public Icon Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Href = Href, MimeType = MimeType};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Icon? other) => other != null && base.Equals(other) && other.Href == Href && other.MimeType == MimeType;

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

    /// <summary>
    /// Provides extensions methods related to <see cref="Icon"/>s.
    /// </summary>
    public static class IconExtensions
    {
        /// <summary>
        /// Returns an icon with a specific mime type if available.
        /// </summary>
        /// <param name="icons">The list of icons to search</param>
        /// <param name="mimeType">The <see cref="Icon.MimeType"/> to try to find. Will only return exact matches.</param>
        /// <returns>The first matching icon that was found or <c>null</c> if no matching icon was found.</returns>
        public static Icon? GetIcon(this IEnumerable<Icon> icons, string mimeType)
            => icons.FirstOrDefault(icon => StringUtils.EqualsIgnoreCase(icon.MimeType, mimeType));
    }
}
