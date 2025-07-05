// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace ZeroInstall.Model;

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

    private static readonly Regex _safeIdRegex = new(@"^[a-zA-Z0-9 ._+\-]+$", RegexOptions.Compiled);

    /// <summary>
    /// Ensures that a value deserialized from an XML attribute is set (not <c>null</c>) and only contains alphanumeric characters, spaces ( ), dots (.), underscores (_), hyphens (-) and plus signs (+).
    /// </summary>
    /// <param name="value">The mapped value to check.</param>
    /// <param name="attributeName">The name of the XML attribute.</param>
    /// <exception cref="InvalidDataException"><paramref name="value"/> is invalid.</exception>
    protected void EnsureAttributeSafeID(string? value, string attributeName)
    {
        if (string.IsNullOrEmpty(value) || !_safeIdRegex.IsMatch(value))
            throw new InvalidDataException($"{string.Format(Resources.InvalidXmlAttributeOnTag, attributeName, ToShortXml())} {Resources.ShouldBeSafeID} {Resources.FoundInstead} {value}");
    }

    /// <summary>
    /// Returns a shortened XML representation (with attributes but without child elements).
    /// </summary>
    /// <remarks>
    /// Intended for use in error messages. Not suitable for parsing.
    /// Use <see cref="XmlStorage.ToXmlString{T}"/> instead if you need a full XML representation.
    /// </remarks>
    public string ToShortXml()
        => this.ToXmlString()
               .Split('\n')[1]
               .Replace($" xmlns=\"{GetType().GetCustomAttribute<XmlRootAttribute>()?.Namespace}\"", "")
               .Replace(" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"", "")
               .Replace(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"", "");

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
            => x != null
            && y != null
            && x.NamespaceURI == y.NamespaceURI && x.Name == y.Name && x.InnerText == y.InnerText
            && x.Attributes.OfType<XmlAttribute>().UnsequencedEquals(y.Attributes.OfType<XmlAttribute>(), comparer: XmlAttributeComparer.Instance)
            && x.ChildNodes.OfType<XmlElement>().SequencedEquals(y.ChildNodes.OfType<XmlElement>(), comparer: Instance);

        public int GetHashCode(XmlElement obj)
            => HashCode.Combine(obj.Name, obj.Value);
    }
    #endregion

    #region Equatable
    public bool Equals(XmlUnknown? other)
        => other != null
        && (UnknownAttributes ?? []).UnsequencedEquals(other.UnknownAttributes ?? [], comparer: XmlAttributeComparer.Instance)
        && (UnknownElements ?? []).SequencedEquals(other.UnknownElements ?? [], comparer: XmlElementComparer.Instance);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(
            (UnknownAttributes ?? []).GetUnsequencedHashCode(XmlAttributeComparer.Instance),
            (UnknownElements ?? []).GetSequencedHashCode(XmlElementComparer.Instance));
    #endregion
}
