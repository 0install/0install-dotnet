// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Builds <see cref="CacheNode"/>s for <see cref="Feed"/>s and <see cref="Implementation"/>s.
/// </summary>
public sealed class CacheNodeBuilder
{
    private readonly ITaskHandler _handler;
    private readonly IFeedCache _feedCache;
    private readonly IImplementationStore? _implementationStore;

    /// <summary>
    /// Creates a new cache node builder.
    /// </summary>
    /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
    /// <param name="feedCache">Used to get local feed files.</param>
    /// <param name="implementationStore">Used to get cached implementations. Leave unset to only list feeds.</param>
    public CacheNodeBuilder(ITaskHandler handler, IFeedCache feedCache, IImplementationStore? implementationStore = null)
    {
        _handler = handler;
        _feedCache = feedCache;
        _implementationStore = implementationStore;
    }

    /// <summary>
    /// Builds a list of <see cref="CacheNode"/>s for <see cref="Feed"/>s and <see cref="Implementation"/>s.
    /// </summary>
    /// <exception cref="IOException">A problem occurred while reading from a cache.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to a cache is not permitted.</exception>
    public NamedCollection<CacheNode> Build()
    {
        var input = new List<object>();
        input.AddRange(_feedCache.ListAll());
        if (_implementationStore != null)
        {
            input.AddRange(_implementationStore.ListAll());
            input.AddRange(_implementationStore.ListTemp());
        }

        var nodes = new NamedCollection<CacheNode>();

        _handler.RunTask(ForEachTask.Create(
            name: Resources.ProcessingFiles,
            target: input,
            work: item =>
            {
                if (item switch
                    {
                        FeedUri uri => GetFeedNode(uri),
                        ManifestDigest digest => GetImplementationNode(digest, nodes.OfType<FeedNode>()),
                        string path => GetTempNode(path),
                        _ => null
                    } is {} node)
                {
                    while (nodes.Contains(node.Name)) node.SuffixCounter++; // Avoid name collisions by incrementing suffix
                    nodes.Add(node);
                }
            }));

        return nodes;
    }

    private FeedNode? GetFeedNode(FeedUri uri)
    {
        try
        {
            string? path = _feedCache.GetPath(uri);
            var feed = _feedCache.GetFeed(uri);
            return path != null && feed != null ? new FeedNode(path, feed) : null;
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Problem building cache node for feed {uri}", ex);
            return null;
        }
        #endregion
    }

    private CacheNode? GetImplementationNode(ManifestDigest digest, IEnumerable<FeedNode> feedNodes)
    {
        if (_implementationStore?.GetPath(digest) is not {} path) return null;

        try
        {
            return (from node in feedNodes
                    from implementation in node.Feed.Implementations
                    where implementation.ManifestDigest.PartialEquals(digest)
                    select new {implementation, node}).FirstOrDefault() is {} found
                ? new OwnedImplementationNode(path, found.implementation, found.node)
                : new ImplementationNode(path, digest);
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            Log.Error($"Problem building cache node for implementation {digest}", ex);
            return null;
        }
        #endregion
    }

    private static CacheNode? GetTempNode(string path)
    {
        try
        {
            return new TempDirectoryNode(path);
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Problem building cache node for temporary directory {path}", ex);
            return null;
        }
        #endregion
    }
}
