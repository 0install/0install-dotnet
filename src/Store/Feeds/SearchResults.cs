// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Net;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Store.Feeds;

/// <summary>
/// Holds results of a feed search query.
/// </summary>
[Serializable, XmlRoot("results"), XmlType("results")]
public class SearchResults
{
    /// <summary>
    /// A list of results matching the specified keywords.
    /// </summary>
    [XmlElement("result")]
    public List<SearchResult> Results { get; } = [];

    /// <summary>
    /// Performs a feed search query using the <see cref="Config.FeedMirror"/>.
    /// </summary>
    /// <param name="config">The current configuration determining which mirror server to query.</param>
    /// <param name="keywords">The keywords to search for.</param>
    /// <exception cref="WebException">Failed to get query result.</exception>
    /// <exception cref="InvalidDataException">Failed to parse query result.</exception>
    public static List<SearchResult> Query(Config config, string? keywords)
    {
        #region Sanity checks
        if (config == null) throw new ArgumentNullException(nameof(config));
        #endregion

        if (config.FeedMirror == null) throw new UriFormatException(Resources.FeedMirrorDisabled);

        if (string.IsNullOrEmpty(keywords)) return new();

        var url = new Uri(
            config.FeedMirror.EnsureTrailingSlash(),
            new Uri($"search/?q={Uri.EscapeDataString(keywords)}", UriKind.Relative));

        Log.Info($"Performing search query: {url.ToStringRfc()}");
        try
        {
            using var httpClient = new HttpClient {Timeout = TimeSpan.FromSeconds(20)};
            using var response = httpClient.Send(new(HttpMethod.Get, url), HttpCompletionOption.ResponseHeadersRead);
            using var stream = response.EnsureSuccessStatusCode().Content.ReadAsStream();
            return XmlStorage.LoadXml<SearchResults>(stream).Results;
        }
        #region Error handling
        catch (HttpRequestException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw ex.AsWebException();
        }
        #endregion
    }
}
