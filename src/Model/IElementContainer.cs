// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// An object that contains <see cref="Group"/>s and <see cref="Implementation"/>s. Supports a composite pattern.
/// </summary>
public interface IElementContainer
{
    /// <summary>
    /// A list of <see cref="Group"/>s and <see cref="Implementation"/>s contained within this element.
    /// </summary>
    [XmlElement(typeof(Implementation)), XmlElement(typeof(PackageImplementation)), XmlElement(typeof(Group))]
    List<Element> Elements { get; }
}
