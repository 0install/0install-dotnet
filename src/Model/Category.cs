// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common;
using ZeroInstall.Model.Design;

namespace ZeroInstall.Model
{
    /// <summary>
    /// An application category (e.g. Game or Office). Used for organizing application menus.
    /// </summary>
    [Description("An application category (e.g. Game or Office). Used for organizing application menus.")]
    [Serializable, XmlRoot("category", Namespace = Feed.XmlNamespace), XmlType("category", Namespace = Feed.XmlNamespace)]
    public sealed class Category : FeedElement, IEquatable<Category>, ICloneable<Category>
    {
        #region Constants
        /// <summary>
        /// Well-known values for <see cref="Name"/> if <see cref="TypeNamespace"/> is empty.
        /// </summary>
        public static readonly string[] WellKnownNames = {"AudioVideo", "Audio", "Video", "Development", "Education", "Game", "Graphics", "Network", "Office", "Science", "Settings", "System", "Utility"};
        #endregion

        /// <summary>
        /// The category name as specified by the <see cref="TypeNamespace"/>.
        /// </summary>
        [Description("The category name as specified by the TypeNamespace.")]
        [TypeConverter(typeof(CategoryNameConverter))]
        [XmlText]
        public string? Name { get; set; }

        /// <summary>
        /// If no type is given, then the category is one of the 'Main' categories defined by the freedesktop.org menu specification (http://standards.freedesktop.org/menu-spec/latest/apa.html). Otherwise, it is a URI giving the namespace for the category.
        /// </summary>
        [Description("If no type is given, then the category is one of the 'Main' categories defined by the freedesktop.org menu specification. Otherwise, it is a URI giving the namespace for the category.")]
        [XmlAttribute("type"), DefaultValue("")]
        public string? TypeNamespace { get; set; }

        #region Conversion
        /// <summary>
        /// Convenience cast for turning strings into <see cref="Category"/>s.
        /// </summary>
        public static implicit operator Category(string value) => new() {Name = value};

        /// <summary>
        /// Returns <see cref="Name"/> directly. Safe for parsing!
        /// </summary>
        public override string ToString() => Name ?? "unset";
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Category? other)
            => other != null
            && base.Equals(other)
            && other.Name == Name
            && other.TypeNamespace == TypeNamespace;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is Category category && Equals(category);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Name, TypeNamespace);
        #endregion

        #region Clone
        /// <summary>
        /// Creates a plain copy of this category.
        /// </summary>
        /// <returns>The cloned category.</returns>
        public Category Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, TypeNamespace = TypeNamespace};
        #endregion
    }
}
