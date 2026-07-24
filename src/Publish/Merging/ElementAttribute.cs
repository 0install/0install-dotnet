// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Xml;

namespace ZeroInstall.Publish.Merging;

/// <summary>
/// Reads and writes a single inheritable attribute of an <see cref="Element"/>.
/// </summary>
/// <param name="Name">The XML name of the attribute, e.g. <c>version</c> or <c>stability</c>.</param>
/// <param name="Get">Gets the current value of the attribute as a boxed object; <c>null</c> if the attribute is not set.</param>
/// <param name="Set">Sets the attribute to a boxed value, or removes it when passed <c>null</c>.</param>
internal sealed record ElementAttribute(string Name, Func<Element, object?> Get, Action<Element, object?> Set)
{
    /// <summary>
    /// The XML name of the <c>main</c> attribute.
    /// </summary>
    public const string MainName = "main";

    /// <summary>
    /// All attributes an <see cref="Element"/> passes on to its children. Mirrors <c>Element.InheritFrom()</c>.
    /// </summary>
    private static readonly ElementAttribute[] _all =
    [
        new("version", x => x.Version, (x, value) => x.Version = (ImplementationVersion?)value),
        new("version-modifier", x => x.VersionModifier, (x, value) => x.VersionModifier = (string?)value),
        new("released", x => x.ReleasedString, (x, value) => x.ReleasedString = (string?)value),
        new("stability", x => x.Stability == Stability.Unset ? null : x.Stability, (x, value) => x.Stability = (Stability?)value ?? Stability.Unset),
        new("rollout-percentage", x => x.RolloutPercentage == 0 ? null : x.RolloutPercentage, (x, value) => x.RolloutPercentage = (int?)value ?? 0),
        new("license", x => x.License, (x, value) => x.License = (string?)value),
        new(MainName, x => x.Main, (x, value) => x.Main = (string?)value),
        new("self-test", x => x.SelfTest, (x, value) => x.SelfTest = (string?)value),
        new("doc-dir", x => x.DocDir, (x, value) => x.DocDir = (string?)value),
        new("langs", x => x.Languages.Count == 0 ? null : x.LanguagesString, (x, value) => x.LanguagesString = (string?)value ?? ""),
        new("arch", x => x.Architecture == default ? null : x.ArchitectureString, (x, value) => x.ArchitectureString = (string?)value ?? new Architecture().ToString())
    ];

    /// <summary>
    /// The key used to identify an unknown (foreign-namespace) attribute.
    /// </summary>
    private static string NameOf(XmlAttribute attribute)
        => $"{attribute.NamespaceURI} {attribute.LocalName}";

    /// <summary>
    /// Collects all attributes of an <see cref="Element"/> into a dictionary, keyed by <see cref="Name"/>. Does not overwrite entries that are already present.
    /// </summary>
    /// <param name="element">The element to read the attributes from.</param>
    /// <param name="attributes">The dictionary to add the attributes to.</param>
    public static void CollectInto(Element element, Dictionary<string, object> attributes)
    {
        foreach (var attribute in _all)
        {
            if (attribute.Get(element) is {} value && !attributes.ContainsKey(attribute.Name))
                attributes[attribute.Name] = value;
        }

        foreach (var unknown in element.UnknownAttributes ?? [])
        {
            string name = NameOf(unknown);
            if (!attributes.ContainsKey(name)) attributes[name] = unknown;
        }
    }

    /// <summary>
    /// Sets the attributes of an <see cref="Element"/> from a dictionary, replacing its unknown attributes with those found there.
    /// </summary>
    /// <param name="element">The element to set the attributes on.</param>
    /// <param name="attributes">The attributes keyed by <see cref="Name"/>.</param>
    public static void SetAll(Element element, Dictionary<string, object> attributes)
    {
        foreach (var attribute in _all)
        {
            if (attributes.TryGetValue(attribute.Name, out var value))
                attribute.Set(element, value);
        }

        element.UnknownAttributes = attributes.Values.OfType<XmlAttribute>().ToArray();
    }

    /// <summary>
    /// Removes attributes from an <see cref="Element"/> whose values are already provided by a dictionary, so they can be inherited instead.
    /// </summary>
    /// <param name="element">The element to remove the redundant attributes from.</param>
    /// <param name="attributes">The attributes already provided, keyed by <see cref="Name"/>.</param>
    public static void RemoveRedundant(Element element, Dictionary<string, object> attributes)
    {
        foreach (var attribute in _all)
        {
            if (attribute.Get(element) is {} value && attributes.TryGetValue(attribute.Name, out var other) && Equals(value, other))
                attribute.Set(element, null);
        }

        element.UnknownAttributes = (element.UnknownAttributes ?? [])
            .Where(x => !attributes.TryGetValue(NameOf(x), out var other)
                        || (other as XmlAttribute)?.Value != x.Value)
            .ToArray();
    }
}
