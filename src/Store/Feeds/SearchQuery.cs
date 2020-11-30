// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Feeds
{
    /// <summary>
    /// Performs a feed search query and stores the response.
    /// </summary>
    [Serializable, XmlRoot("results"), XmlType("results")]
    public class SearchQuery
    {
        /// <summary>
        /// The keywords the search was performed for.
        /// </summary>
        [XmlIgnore]
        public string? Keywords { get; private set; }

        /// <summary>
        /// A list of results matching the <see cref="Keywords"/>.
        /// </summary>
        [XmlElement("result")]
        public List<SearchResult> Results { get; } = new();

        /// <summary>
        /// Performs a feed search query using the <see cref="Config.FeedMirror"/>.
        /// </summary>
        /// <param name="config">The current configuration determining which mirror server to query.</param>
        /// <param name="keywords">The keywords to search for.</param>
        public static SearchQuery Perform(Config config, string? keywords)
        {
            #region Sanity checks
            if (config == null) throw new ArgumentNullException(nameof(config));
            #endregion

            if (config.FeedMirror == null) throw new UriFormatException(Resources.FeedMirrorDisabled);

            if (string.IsNullOrEmpty(keywords)) return new SearchQuery();

            var url = new Uri(
                config.FeedMirror.EnsureTrailingSlash(),
                new Uri("search/?q=" + Uri.EscapeUriString(keywords), UriKind.Relative));

            Log.Info("Performing search query: " + url.ToStringRfc());
            using var webClient = new WebClientTimeout();
            var result = XmlStorage.FromXmlString<SearchQuery>(webClient.DownloadString(url));
            result.Keywords = keywords;
            return result;
        }

        /// <summary>
        /// Returns the query in the form "Feed search: Keywords". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"Feed search: {Keywords}";
    }
}
