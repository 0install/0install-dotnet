// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Feeds;

/// <summary>
/// A single result of a feed search.
/// </summary>
[Serializable, XmlType("result")]
[Equatable]
public partial class SearchResult
{
    /// <summary>
    /// The URI of the feed.
    /// </summary>
    [DisplayName("URI")]
    [XmlIgnore]
    public required FeedUri Uri { get; set; }

    #region XML serialization
    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="Uri"/>
    [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
    [Browsable(false)]
    [XmlAttribute("uri"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), IgnoreEquality]
    public string UriString { get => Uri.ToStringRfc(); set => Uri = new(value); }
    #endregion

    /// <summary>
    /// A short name to identify the interface (e.g. "Foo").
    /// </summary>
    [XmlAttribute("name")]
    public required string Name { get; set; }

    /// <summary>
    /// A value between 0 and 100 indicating how good this result matches the query.
    /// </summary>
    [XmlAttribute("score")]
    public int Score { get; set; }

    /// <summary>
    /// Short one-line description for different languages; the first word should not be upper-case unless it is a proper noun (e.g. "cures all ills").
    /// </summary>
    [XmlElement("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// A list of well-known categories the applications fits into.
    /// </summary>
    [Browsable(false)]
    [XmlElement("category")]
    public List<Category> Categories { get; } = new();

    /// <summary>Used for DataGrid rendering.</summary>
    /// <seealso cref="Categories"/>
    [DisplayName("Categories"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [XmlIgnore, IgnoreEquality]
    public string CategoriesString
        => string.Join(", ", Categories.Select(x => x.Name).WhereNotNull());

    /// <summary>
    /// Generates a pseudo-<see cref="Feed"/> using the information from this result.
    /// </summary>
    /// <returns>A pseudo-<see cref="Feed"/>; not a complete feed that can be used to launch an implementation.</returns>
    public Feed ToPseudoFeed()
    {
        var feed = new Feed
        {
            Uri = Uri,
            Name = Name,
            Categories = {Categories.CloneElements()}
        };
        if (!string.IsNullOrEmpty(Summary)) feed.Summaries.Add(Summary);
        return feed;
    }

    /// <summary>
    /// Creates string representation suitable for console output.
    /// </summary>
    public override string ToString()
        => $"{Uri.ToStringRfc()}: {Name} - {Summary} [{Score}%]";
}
