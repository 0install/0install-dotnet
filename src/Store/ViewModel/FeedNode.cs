// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Models information about a <see cref="Feed"/> in the <see cref="IFeedCache"/> for display in a UI.
/// </summary>
public sealed class FeedNode : CacheNode
{
    /// <summary>
    /// Creates a new feed node.
    /// </summary>
    /// <param name="path">The path of the directory.</param>
    /// <param name="feed">The parsed feed.</param>
    /// <exception cref="IOException">The feed file could not be read.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the feed file is not permitted.</exception>
    public FeedNode(string path, Feed feed)
        : base(path, new FileInfo(path).Length)
    {
        Feed = feed;
    }

    /// <summary>
    /// The parsed feed.
    /// </summary>
    [Browsable(false)]
    public Feed Feed { get; }

    /// <inheritdoc/>
    public override string Name
    {
        get
        {
            var builder = new StringBuilder(Feed.Name);
            if (Feed.FeedFor.Count != 0) builder.Append(" (feed-for)");
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
    public FeedUri Uri => Feed.Uri ?? new("http://missing");

    /// <summary>
    /// The main website of the application.
    /// </summary>
    [Description("The main website of the application.")]
    public Uri? Homepage => Feed.Homepage;

    /// <summary>
    /// A short one-line description of the application.
    /// </summary>
    [Description("A short one-line description of the application.")]
    public string? Summary => Feed.Summaries.GetBestLanguage(CultureInfo.CurrentUICulture);

    /// <summary>
    /// A comma-separated list of categories the applications fits into.
    /// </summary>
    [Description("A comma-separated list of categories the applications fits into.")]
    public string Categories => string.Join(",", Feed.Categories.Select(x => x.Name).WhereNotNull());

    /// <summary>
    /// Removes this <see cref="Feed"/> from the <paramref name="feedCache"/> if provided.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No matching feed could be found in the <see cref="IFeedCache"/>.</exception>
    /// <exception cref="IOException">The feed could not be deleted.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the cache is not permitted.</exception>
    public override void Remove(IFeedCache? feedCache = null, IImplementationStore? implementationStore = null)
        => feedCache?.Remove(Uri);

    /// <inheritdoc/>
    public override bool Equals(CacheNode? other)
        => other is FeedNode feedNode && feedNode.Uri == Uri;

    /// <inheritdoc/>
    public override int GetHashCode() => Uri.GetHashCode();

    /// <summary>
    /// Creates string representation suitable for console output.
    /// </summary>
    public override string ToString()
        => Uri.ToStringRfc();
}
