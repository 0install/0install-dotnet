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

    /// <summary>
    /// Removes an <see cref="Implementation"/> identified by its ID.
    /// </summary>
    /// <returns><c>true</c> if the implementation was removed; <c>false</c> if the implementation could not be found.</returns>
    public static bool RemoveImplementation(this ICollection<Element> elements, string id)
        => elements.RemoveAll(x => x is Implementation implementation && implementation.ID == id)
        || elements.OfType<Group>().Any(group => @group.Elements.RemoveImplementation(id));
}
