// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;
using ZeroInstall.Model;

namespace ZeroInstall.Store.Feeds
{
    /// <summary>
    /// A single result of a feed search.
    /// </summary>
    [Serializable, XmlType("result")]
    public class SearchResult
    {
        /// <summary>
        /// The URI of the feed.
        /// </summary>
        [XmlIgnore]
        public FeedUri Uri { get; set; } = default!;

        #region XML serialization
        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="Uri"/>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
        [Browsable(false)]
        [XmlAttribute("uri"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        public string UriString { get => Uri.ToStringRfc(); set => Uri = new FeedUri(value); }
        #endregion

        /// <summary>
        /// A short name to identify the interface (e.g. "Foo").
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; } = default!;

        /// <summary>
        /// A value between 0 and 100 indicating how good this result matches the query.
        /// </summary>
        [XmlAttribute("score")]
        public int Score { get; set; }

        /// <summary>
        /// Short one-line description for different languages; the first word should not be upper-case unless it is a proper noun (e.g. "cures all ills").
        /// </summary>
        [XmlElement("summary")]
        public string Summary { get; set; } = default!;

        /// <summary>
        /// A list of well-known categories the applications fits into.
        /// </summary>
        [Browsable(false)]
        [XmlElement("category")]
        public List<Category> Categories { get; } = new List<Category>();

        /// <summary>Used for DataGrid rendering.</summary>
        /// <seealso cref="Categories"/>
        [XmlIgnore, DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        public string CategoriesString
            => StringUtils.Join(", ", Categories.Select(x => x.Name).WhereNotNull());

        /// <summary>
        /// Generates a pseudo-<see cref="Feed"/> using the information from this result.
        /// </summary>
        /// <returns>A pseudo-<see cref="Feed"/>; not a complete feed that can be used to launch an implementation.</returns>
        public Feed ToPseudoFeed()
        {
            var feed = new Feed {Uri = Uri, Name = Name, Summaries = {Summary}};
            feed.Categories.AddRange(Categories.CloneElements());
            return feed;
        }

        /// <summary>
        /// Returns the result in the form "Uri NEWLINE Name - Summary [Score]". Not safe for parsing!
        /// </summary>
        public override string ToString() => Uri.ToStringRfc() + Environment.NewLine + $"{Name} - {Summary} [{Score}%]";
    }
}
