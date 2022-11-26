// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Models information about an implementation in an <see cref="IImplementationStore"/> for display in a UI.
/// </summary>
public class ImplementationNode : CacheNode
{
    /// <summary>
    /// Creates a new implementation node.
    /// </summary>
    /// <param name="path">The path of the directory.</param>
    /// <param name="digest">The digest identifying the implementation.</param>
    /// <exception cref="IOException">The manifest file could not be read.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the manifest file is not permitted.</exception>
    /// <exception cref="FormatException">The manifest file is not valid.</exception>
    public ImplementationNode(string path, ManifestDigest digest)
        : base(path, GetSize(path, digest))
    {
        _digest = digest;
    }

    private static long GetSize(string path, ManifestDigest digest)
        => Manifest.TryLoad(
               System.IO.Path.Combine(path, Manifest.ManifestFile),
               ManifestFormat.FromPrefix(digest.AvailableDigests.First()))?.TotalSize
        ?? 0;

    /// <inheritdoc/>
    public override string Name { get => Resources.UnknownInterface + Named.TreeSeparator + Digest + (SuffixCounter == 0 ? "" : " " + SuffixCounter); set => throw new NotSupportedException(); }

    private readonly ManifestDigest _digest;

    /// <summary>
    /// The digest identifying the implementation in the store.
    /// </summary>
    [Description("The digest identifying the implementation in the store.")]
    public string? Digest => _digest.Best;

    /// <summary>
    /// The URI of the feed describing the implementation.
    /// </summary>
    [DisplayName("Feed URI"), Description("The URI of the feed describing the implementation.")]
    public virtual FeedUri? FeedUri => null;

    /// <summary>
    /// Removes this implementation from the <paramref name="implementationStore"/> if provided.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No matching implementation could be found in the <see cref="IImplementationStore"/>.</exception>
    /// <exception cref="IOException">The implementation could not be deleted.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the store is not permitted.</exception>
    public override void Remove(IFeedCache? feedCache = null, IImplementationStore? implementationStore = null)
        => implementationStore?.Remove(_digest);

    /// <summary>
    /// Verify this implementation is undamaged.
    /// </summary>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">The entry's directory could not be processed.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the entry's directory is not permitted.</exception>
    public void Verify(IImplementationStore implementationStore)
        => implementationStore.Verify(_digest);

    /// <inheritdoc/>
    public override bool Equals(CacheNode? other)
        => other is ImplementationNode implementationNode && implementationNode.Digest == Digest;

    /// <inheritdoc/>
    public override int GetHashCode() => Digest?.GetHashCode() ?? 0;

    /// <summary>
    /// Creates string representation suitable for console output.
    /// </summary>
    public override string ToString()
        => $"{Digest}: {base.ToString()}";
}
