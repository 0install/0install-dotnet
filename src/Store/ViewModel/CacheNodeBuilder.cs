// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Builds a list of <see cref="CacheNode"/>s for <see cref="Feed"/>s and <see cref="Implementation"/>s.
/// </summary>
[PrimaryConstructor]
public sealed partial class CacheNodeBuilder : TaskBase
{
    private readonly IImplementationStore _implementationStore;
    private readonly IFeedCache _feedCache;

    /// <inheritdoc/>
    public override string Name => "Loading";

    /// <inheritdoc/>
    protected override bool UnitsByte => false;

    /// <summary>
    /// All generated nodes.
    /// </summary>
    public NamedCollection<CacheNode>? Nodes { get; private set; }

    /// <summary>
    /// The total size of all <see cref="Implementation"/>s in bytes.
    /// </summary>
    public long TotalSize { get; private set; }

    private IEnumerable<Feed>? _feeds;

    /// <inheritdoc/>
    protected override void Execute()
    {
        Nodes = new NamedCollection<CacheNode>();
        _feeds = _feedCache.GetAll();

        foreach (var feed in _feeds)
            Add(GetFeedNode(feed));

        foreach (var digest in _implementationStore.ListAll())
        {
            var node = GetImplementationNode(digest);
            if (node != null)
            {
                TotalSize += node.Size;
                Add(node);
            }
        }

        foreach (string path in _implementationStore.ListAllTemp())
            Add(GetTempNode(path));
    }

    private FeedNode GetFeedNode(Feed feed)
    {
        feed.Normalize(feed.Uri);
        var node = new FeedNode(feed, _feedCache);
        return node;
    }

    private ImplementationNode? GetImplementationNode(ManifestDigest digest)
    {
        Debug.Assert(_feeds != null);

        try
        {
            var found = _feeds.FindImplementation(digest);
            if (found.HasValue)
                return new OwnedImplementationNode(digest, found.Value.implementation, new FeedNode(found.Value.feed, _feedCache), _implementationStore);
            else
                return new OrphanedImplementationNode(digest, _implementationStore);
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            Log.Error($"Problem processing '{digest}'.");
            Log.Error(ex);
            return null;
        }
        #endregion
    }

    private TempDirectoryNode GetTempNode(string path)
        => new(path, _implementationStore);

    private void Add(CacheNode node)
    {
        Debug.Assert(Nodes != null);

        // Avoid name collisions by incrementing suffix
        while (Nodes.Contains(node.Name)) node.SuffixCounter++;

        Nodes.Add(node);
    }
}
