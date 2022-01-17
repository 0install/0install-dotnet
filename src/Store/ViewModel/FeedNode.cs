// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using ZeroInstall.Store.Feeds;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Models information about a <see cref="Feed"/> in the <see cref="IFeedCache"/> for display in a UI.
/// </summary>
[PrimaryConstructor]
public sealed partial class FeedNode : CacheNode
{
    private readonly Feed _feed;
    private readonly IFeedCache _cache;

    /// <inheritdoc/>
    public override string Name
    {
        get
        {
            var builder = new StringBuilder(_feed.Name);
            if (_feed.FeedFor.Count != 0) builder.Append(" (feed-for)");
            if (SuffixCounter != 0)
            {
                builder.Append(' ');
                builder.Append(SuffixCounter);
            }
            return builder.ToString();
        }
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// The URI identifying this feed.
    /// </summary>
    [Description("The URI identifying this feed.")]
    public FeedUri? Uri => _feed.Uri;

    /// <summary>
    /// The main website of the application.
    /// </summary>
    [Description("The main website of the application.")]
    public Uri? Homepage => _feed.Homepage;

    /// <summary>
    /// A short one-line description of the application.
    /// </summary>
    [Description("A short one-line description of the application.")]
    public string? Summary => _feed.Summaries.GetBestLanguage(CultureInfo.CurrentUICulture);

    /// <summary>
    /// A comma-separated list of categories the applications fits into.
    /// </summary>
    [Description("A comma-separated list of categories the applications fits into.")]
    public string Categories => StringUtils.Join(",", _feed.Categories.Select(x => x.Name).WhereNotNull());

    /// <summary>
    /// Deletes this <see cref="Feed"/> from the <see cref="IFeedCache"/> it is located in.
    /// </summary>
    /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about IO tasks.</param>
    /// <exception cref="KeyNotFoundException">No matching feed could be found in the <see cref="IFeedCache"/>.</exception>
    /// <exception cref="IOException">The feed could not be deleted.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the cache is not permitted.</exception>
    public override void Delete(ITaskHandler handler)
    {
        if (_feed.Uri != null) _cache.Remove(_feed.Uri);
    }
}
