// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using ZeroInstall.Model.Properties;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Abstract base class for XML serializable classes that are intended to retain any unknown XML elements or attributes loaded from an XML file.
    /// </summary>
    /// <remarks>Inheriting from this class will prevent the <see cref="XmlSerializer.UnknownElement"/> event from being triggered.</remarks>
    public abstract class XmlUnknown : IEquatable<XmlUnknown>
    {
        /// <summary>
        /// Contains any unknown additional XML attributes.
        /// </summary>
        [XmlAnyAttribute]
        public XmlAttribute[]? UnknownAttributes;

        /// <summary>
        /// Contains any unknown additional XML elements.
        /// </summary>
        [XmlAnyElement]
        public XmlElement[]? UnknownElements;

        /// <summary>
        /// Ensures that a value deserialized from an XML attribute is set (not <c>null</c>).
        /// </summary>
        /// <param name="value">The mapped value to check.</param>
        /// <param name="attributeName">The name of the XML attribute.</param>
        /// <exception cref="InvalidDataException"><paramref name="value"/> is <c>null</c>.</exception>
        protected void EnsureAttribute(object? value, string attributeName)
        {
            if (value == null)
                throw new InvalidDataException(string.Format(Resources.MissingXmlAttributeOnTag, attributeName, ToShortXml()));
        }

        /// <summary>
        /// Ensures that a value deserialized from an XML attribute is set (not <c>null</c>) and only contains alphanumeric characters, spaces ( ), dots (.), underscores (_), hyphens (-) and plus signs (+).
        /// </summary>
        /// <param name="value">The mapped value to check.</param>
        /// <param name="attributeName">The name of the XML attribute.</param>
        /// <exception cref="InvalidDataException"><paramref name="value"/> is invalid.</exception>
        protected void EnsureAttributeSafeID(string? value, string attributeName)
        {
            if (string.IsNullOrEmpty(value) || !value.All(x => char.IsLetterOrDigit(x) || x is ' ' or '.' or '_' or '-' or '+'))
                throw new InvalidDataException(string.Format(Resources.InvalidXmlAttributeOnTag, attributeName, ToShortXml()) + " " + Resources.ShouldBeSafeID + " " + Resources.FoundInstead + " " + value);
        }

        /// <summary>
        /// Returns a shortened XML representation (with attributes but without child elements).
        /// </summary>
        /// <remarks>
        /// Intended for use in error messages. Not suitable for parsing.
        /// Use <see cref="XmlStorage.ToXmlString{T}"/> instead if you need a full XML representation.
        /// </remarks>
        public string ToShortXml()
        {
            var type = GetType();
            using var writer = new StringWriter {NewLine = "\n"};
            new XmlSerializer(type).Serialize(new XmlTextWriter(writer) {Formatting = Formatting.Indented}, this);
            return writer.ToString()
                         .Split('\n')[1]
                         .Replace($" xmlns=\"{type.GetCustomAttribute<XmlRootAttribute>()?.Namespace}\"", "")
                         .Replace(" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"", "")
                         .Replace(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"", "");
        }

        #region Comparers
        private class XmlAttributeComparer : IEqualityComparer<XmlAttribute>
        {
            public static readonly XmlAttributeComparer Instance = new();

            public bool Equals(XmlAttribute? x, XmlAttribute? y)
            {
                if (x == null || y == null) return false;
                return x.NamespaceURI == y.NamespaceURI && x.Name == y.Name && x.Value == y.Value;
            }

            public int GetHashCode(XmlAttribute obj)
                => HashCode.Combine(obj.Name, obj.Value);
        }

        private class XmlElementComparer : IEqualityComparer<XmlElement>
        {
            public static readonly XmlElementComparer Instance = new();

            public bool Equals(XmlElement? x, XmlElement? y)
            {
                if (x == null || y == null) return false;
                if (x.NamespaceURI != y.NamespaceURI || x.Name != y.Name || x.InnerText != y.InnerText) return false;

                // ReSharper disable once InvokeAsExtensionMethod
                bool attributesEqual = EnumerableExtensions.UnsequencedEquals(
                    x.Attributes.OfType<XmlAttribute>().ToArray(),
                    y.Attributes.OfType<XmlAttribute>().ToArray(),
                    comparer: XmlAttributeComparer.Instance);
                bool elementsEqual = EnumerableExtensions.SequencedEquals(
                    x.ChildNodes.OfType<XmlElement>().ToArray(),
                    y.ChildNodes.OfType<XmlElement>().ToArray(),
                    comparer: Instance);
                return attributesEqual && elementsEqual;
            }

            public int GetHashCode(XmlElement obj)
                => HashCode.Combine(obj.Name, obj.Value);
        }
        #endregion

        #region Equatable
        public bool Equals(XmlUnknown? other)
        {
            if (other == null) return false;
            // ReSharper disable once InvokeAsExtensionMethod
            bool attributesEqual = EnumerableExtensions.UnsequencedEquals(
                UnknownAttributes ?? Array.Empty<XmlAttribute>(),
                other.UnknownAttributes ?? Array.Empty<XmlAttribute>(),
                comparer: XmlAttributeComparer.Instance);
            bool elementsEqual = EnumerableExtensions.SequencedEquals(
                UnknownElements ?? Array.Empty<XmlElement>(),
                other.UnknownElements ?? Array.Empty<XmlElement>(),
                comparer: XmlElementComparer.Instance);
            return attributesEqual && elementsEqual;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                (UnknownAttributes ?? Array.Empty<XmlAttribute>()).GetUnsequencedHashCode(XmlAttributeComparer.Instance),
                (UnknownElements ?? Array.Empty<XmlElement>()).GetSequencedHashCode(XmlElementComparer.Instance));
        #endregion
    }
}
