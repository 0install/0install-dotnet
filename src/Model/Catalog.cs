// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Contains a list of <see cref="Feed"/>s, reduced to only contain information relevant for overview lists.
/// </summary>
/// <remarks>
/// See also: https://docs.0install.net/specifications/catalog/
/// </remarks>
[Description("Contains a list of feeds, reduced to only contain information relevant for overview lists.")]
[Serializable, XmlRoot("catalog", Namespace = XmlNamespace), XmlType("catalog", Namespace = XmlNamespace)]
[XmlNamespace("xsi", XmlStorage.XsiNamespace)]
//[XmlNamespace("feed", Feed.XmlNamespace)]
[Equatable]
public partial class Catalog : XmlUnknown, ICloneable<Catalog>
{
    #region Constants
    /// <summary>
    /// The XML namespace used for storing feed catalogs. Used in combination with <see cref="Feed.XmlNamespace"/>.
    /// </summary>
    public const string XmlNamespace = "http://0install.de/schema/injector/catalog";

    /// <summary>
    /// The URI to retrieve an XSD containing the XML Schema information for this class in serialized form.
    /// </summary>
    public const string XsdLocation = "https://docs.0install.net/specifications/catalog.xsd";

    /// <summary>
    /// Provides XML Editors with location hints for XSD files.
    /// </summary>
    public const string XsiSchemaLocation = $"{XmlNamespace} {XsdLocation}";

    /// <summary>
    /// Provides XML Editors with location hints for XSD files.
    /// </summary>
    [XmlAttribute("schemaLocation", Namespace = XmlStorage.XsiNamespace)]
    public string SchemaLocation = XsiSchemaLocation;
    #endregion

    /// <summary>
    /// A list of <see cref="Feed"/>s contained within this catalog.
    /// </summary>
    [Browsable(false)]
    [XmlElement("interface", typeof(Feed), Namespace = Feed.XmlNamespace)]
    [OrderedEquality]
    public List<Feed> Feeds { get; } = new();

    /// <summary>
    /// Determines whether this catalog contains a <see cref="Feed"/> with a specific URI.
    /// </summary>
    /// <param name="uri">The <see cref="Feed.Uri"/> to look for.</param>
    /// <returns><c>true</c> if a matching feed was found; <c>false</c> otherwise.</returns>
    public bool ContainsFeed(FeedUri uri)
    {
        #region Sanity checks
        if (uri == null) throw new ArgumentNullException(nameof(uri));
        #endregion

        return Feeds.Any(feed => feed.Uri == uri);
    }

    /// <summary>
    /// Returns the <see cref="Feed"/> with a specific URI.
    /// </summary>
    /// <param name="uri">The <see cref="Feed.Uri"/> to look for.</param>
    /// <returns>The identified <see cref="Feed"/>.</returns>
    /// <exception cref="KeyNotFoundException">No <see cref="Feed"/> matching <paramref name="uri"/> was found in <see cref="Feeds"/>.</exception>
    public Feed this[FeedUri uri]
        => Feeds.FirstOrDefault(feed => feed.Uri == uri)
        ?? throw new KeyNotFoundException(string.Format(Resources.FeedNotInCatalog, uri));

    /// <summary>
    /// Returns the <see cref="Feed"/> with a specific URI. Safe for missing elements.
    /// </summary>
    /// <param name="uri">The <see cref="Feed.Uri"/> to look for.</param>
    /// <returns>The identified <see cref="Feed"/>; <c>null</c> if no matching entry was found.</returns>
    public Feed? GetFeed(FeedUri uri)
    {
        #region Sanity checks
        if (uri == null) throw new ArgumentNullException(nameof(uri));
        #endregion

        return Feeds.FirstOrDefault(feed => feed.Uri == uri);
    }

    /// <summary>
    /// Returns the first <see cref="Feed"/> that matches a specific short name.
    /// </summary>
    /// <param name="shortName">The short name to look for. Must match either <see cref="Feed.Name"/> or <see cref="EntryPoint.BinaryName"/> of <see cref="Command.NameRun"/>.</param>
    /// <returns>The first matching <see cref="Feed"/>; <c>null</c> if no match was found.</returns>
    public Feed? FindByShortName(string? shortName)
    {
        if (string.IsNullOrEmpty(shortName)) return null;

        foreach (var feed in Feeds.Where(x => x.Uri != null && !string.IsNullOrEmpty(x.Name)))
        {
            if (StringUtils.EqualsIgnoreCase(feed.Name, shortName)) return feed;
            if (StringUtils.EqualsIgnoreCase(feed.Name.Replace(' ', '-'), shortName)) return feed;

            var entryPoint = feed.GetEntryPoint(Command.NameRun);
            if (!string.IsNullOrEmpty(entryPoint?.BinaryName) && StringUtils.EqualsIgnoreCase(entryPoint.BinaryName, shortName))
                return feed;
        }

        return null;
    }

    /// <summary>
    /// Returns all <see cref="Feed"/>s that match a specific search query.
    /// </summary>
    /// <param name="query">The search query. Must be contained within <see cref="Feed.Name"/> or <see cref="EntryPoint.BinaryName"/> of <see cref="Command.NameRun"/>.</param>
    /// <returns>All <see cref="Feed"/>s matching <paramref name="query"/>.</returns>
    public IEnumerable<Feed> Search(string? query)
    {
        if (string.IsNullOrEmpty(query))
        {
            foreach (var feed in Feeds)
                yield return feed;
        }
        else
        {
            foreach (var feed in Feeds)
            {
                if (feed.Uri != null && !string.IsNullOrEmpty(feed.Name))
                {
                    if (feed.Name.ContainsIgnoreCase(query)) yield return feed;
                    else if (feed.Name.Replace(' ', '-').ContainsIgnoreCase(query)) yield return feed;
                }
            }
        }
    }

    #region Normalize
    /// <summary>
    /// Normalizes the catalog and all feeds it contains.
    /// Flattens inheritance structures, converts legacy elements, sets default values, etc..
    /// </summary>
    /// <param name="catalogUri">The URI the catalog was originally loaded from.</param>
    /// <exception cref="NotSupportedException">The catalog requires a newer version of Zero Install.</exception>
    /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
    public void Normalize(FeedUri? catalogUri = null)
    {
        foreach (var feed in Feeds)
        {
            feed.Normalize(feed.Uri);
            feed.CatalogUri = catalogUri;
        }
    }
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="Catalog"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Catalog"/>.</returns>
    public Catalog Clone() => new()
    {
        UnknownAttributes = UnknownAttributes,
        UnknownElements = UnknownElements,
        Feeds = {Feeds.CloneElements()}
    };
    #endregion
}
