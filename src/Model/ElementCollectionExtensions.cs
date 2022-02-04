// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Provides extension methods for <see cref="Element"/> collections.
/// </summary>
public static class ElementCollectionExtensions
{
    /// <summary>
    /// Returns a flat list of all <see cref="Implementation"/>s.
    /// </summary>
    public static IEnumerable<Implementation> GetImplementations(this IReadOnlyCollection<Element> elements)
        => elements.OfType<Implementation>()
                   .Concat(elements.OfType<Group>().SelectMany(x => x.Elements.GetImplementations()));
}
