// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Models information about a temporary directory in an <see cref="IImplementationStore"/> for display in a UI.
/// </summary>
public sealed class TempDirectoryNode : CacheNode
{
    /// <summary>
    /// Creates a new temporary directory node.
    /// </summary>
    /// <param name="path">The path of the directory.</param>
    /// <exception cref="IOException">The directory could not be inspected.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the directory is not permitted.</exception>
    public TempDirectoryNode(string path)
        : base(path, GetSize(path))
    {}

    private static long GetSize(string path)
    {
        long size = 0;
        new DirectoryInfo(path).Walk(fileAction: file => size += file.Length);
        return size;
    }

    /// <inheritdoc/>
    public override string Name
    {
        get => Resources.TemporaryDirectories + Named.TreeSeparator + System.IO.Path.GetFileName(Path) + (SuffixCounter == 0 ? "" : $" {SuffixCounter}");
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Removes this temporary directory from the <paramref name="implementationStore"/> if provided.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">The directory could be found in the store.</exception>
    /// <exception cref="IOException">The directory could not be deleted.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the store is not permitted.</exception>
    public override void Remove(IFeedCache? feedCache = null, IImplementationStore? implementationStore = null)
        => implementationStore?.RemoveTemp(Path);

    /// <inheritdoc/>
    public override bool Equals(CacheNode? other)
        => other is TempDirectoryNode tempNode && tempNode.Path == Path;

    /// <inheritdoc/>
    public override int GetHashCode() => Path.GetHashCode();
}
