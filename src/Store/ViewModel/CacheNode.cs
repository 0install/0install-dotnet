// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Models information about elements in a cache for display in a UI.
/// </summary>
/// <param name="path">The path of the directory.</param>
/// <param name="size">The total size of the directory in bytes.</param>
[SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes", Justification = "Comparison only used for INamed sorting")]
public abstract class CacheNode(string path, long size) : INamed, IEquatable<CacheNode>
{
    /// <summary>
    /// The full name of the node used for tree hierarchies.
    /// </summary>
    [Browsable(false)]
    public abstract string Name { get; set; }

    /// <summary>
    /// A counter that can be used to prevent naming collisions.
    /// </summary>
    /// <remarks>If this value is not zero it is appended to the <see cref="Name"/>.</remarks>
    [Browsable(false)]
    public int SuffixCounter { get; internal set; }

    /// <summary>
    /// The path of the directory.
    /// </summary>
    [Description("The path of the directory.")]
    public string Path { get; } = path;

    /// <summary>
    /// The total size of the directory in bytes.
    /// </summary>
    [Browsable(false)]
    public long Size { get; } = size;

    /// <summary>
    /// The total size of the directory in human-readable form.
    /// </summary>
    [DisplayName("Size"), Description("The total size of the directory.")]
    public string SizeHuman => Size.FormatBytes();

    /// <summary>
    /// Removes this element from the cache it is stored in.
    /// </summary>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="KeyNotFoundException">No matching element could be found in the cache.</exception>
    /// <exception cref="IOException">The element could not be deleted.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the cache is not permitted.</exception>
    public abstract void Remove(IFeedCache? feedCache = null, IImplementationStore? implementationStore = null);

    /// <inheritdoc/>
    public abstract bool Equals(CacheNode? other);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj == this) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((CacheNode)obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Creates string representation suitable for console output.
    /// </summary>
    public override string ToString()
        => Path;
}
