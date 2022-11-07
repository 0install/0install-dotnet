// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Combines multiple <see cref="IImplementationStore"/>s as a composite.
/// </summary>
/// <remarks>
///   <para>When adding new <see cref="Implementation"/>s the last child <see cref="IImplementationStore"/> that doesn't throw an <see cref="UnauthorizedAccessException"/> is used.</para>
///   <para>When when retrieving existing <see cref="Implementation"/>s the first child <see cref="IImplementationStore"/> that returns <c>true</c> for <see cref="IImplementationSink.Contains(ManifestDigest)"/> is used.</para>
/// </remarks>
public class CompositeImplementationStore : CompositeImplementationSink, IImplementationStore
{
    /// <summary>
    /// The <see cref="IImplementationStore"/>s this store is internally composed of.
    /// </summary>
    public IReadOnlyList<IImplementationStore> Stores { get; }

    /// <summary>
    /// Creates a new composite implementation store with a set of <see cref="IImplementationStore"/>s.
    /// </summary>
    /// <param name="stores">
    ///   A priority-sorted list of <see cref="IImplementationStore"/>s.
    ///   Queried last-to-first for adding new <see cref="Implementation"/>s, first-to-last otherwise.
    /// </param>
    public CompositeImplementationStore(IReadOnlyList<IImplementationStore> stores)
        : base(stores)
    {
        Stores = stores;
    }

    /// <inheritdoc/>
    public ImplementationStoreKind Kind => ImplementationStoreKind.ReadWrite;

    /// <inheritdoc/>
    public string Path
        => string.Join(System.IO.Path.PathSeparator.ToString(), Stores.Select(x => x.Path));

    /// <inheritdoc/>
    public string? GetPath(ManifestDigest manifestDigest)
        => Stores.TrySelect(store => store.GetPath(manifestDigest), (UnauthorizedAccessException _) => {})
                 .WhereNotNull()
                 .FirstOrDefault();

    /// <inheritdoc/>
    public IEnumerable<ManifestDigest> ListAll()
        // Merge the lists from all contained stores, eliminating duplicates
        => Stores.TrySelect(x => x.ListAll(), (UnauthorizedAccessException _) => {})
                 .SelectMany(x => x)
                 .Distinct();

    /// <inheritdoc/>
    public IEnumerable<string> ListTemp()
        // Merge the lists from all contained stores, eliminating duplicates
        => Stores.TrySelect(x => x.ListTemp(), (UnauthorizedAccessException _) => {})
                 .SelectMany(x => x);

    /// <inheritdoc />
    public void Verify(ManifestDigest manifestDigest)
    {
        Exception? lastException = null;
        foreach (var store in Stores.Where(x => x.Kind != ImplementationStoreKind.Service))
        {
            try
            {
                store.Verify(manifestDigest);
                return;
            }
            catch (ImplementationNotFoundException ex)
            { // Ignore "not found" errors unless it was the last store
                lastException = ex;
            }
        }
        lastException?.Rethrow();
    }

    /// <inheritdoc/>
    public bool Remove(ManifestDigest manifestDigest)
    {
        // Remove from _every_ store that contains the implementation
        bool removed = false;
        foreach (var store in Stores.Reverse())
            removed |= store.Remove(manifestDigest);

        return removed;
    }

    /// <inheritdoc/>
    public bool RemoveTemp(string path)
        => Stores.Reverse().Any(x => x.RemoveTemp(path));

    /// <inheritdoc />
    public void Purge()
    {
        foreach (var store in Stores.Reverse())
            store.Purge();
    }

    /// <inheritdoc/>
    public long Optimise()
    {
        // Try to optimize all contained stores
        return Stores.Reverse().Sum(x => x.Optimise());
    }

    /// <summary>
    /// Creates string representation suitable for console output.
    /// </summary>
    public override string ToString()
        => "CompositeStore: " + string.Join(", ", Stores.Select(x => x.ToString()).WhereNotNull());
}
