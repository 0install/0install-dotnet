// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.ObjectModel;
using ZeroInstall.Store.FileSystem;
#if NETFRAMEWORK
using System.Runtime.Remoting;
#endif

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Combines multiple <see cref="IImplementationStore"/>s as a composite.
/// </summary>
/// <remarks>
///   <para>When adding new <see cref="Implementation"/>s the last child <see cref="IImplementationStore"/> that doesn't throw an <see cref="UnauthorizedAccessException"/> is used.</para>
///   <para>When when retrieving existing <see cref="Implementation"/>s the first child <see cref="IImplementationStore"/> that returns <c>true</c> for <see cref="IImplementationStore.Contains(ManifestDigest)"/> is used.</para>
/// </remarks>
public class CompositeImplementationStore : MarshalByRefObject, IImplementationStore
{
    private readonly IImplementationStore[] _innerStores;

    /// <summary>
    /// The <see cref="IImplementationStore"/>s this store is internally composed of.
    /// </summary>
    public IEnumerable<IImplementationStore> Stores => new ReadOnlyCollection<IImplementationStore>(_innerStores);

    /// <summary>
    /// Creates a new composite implementation store with a set of <see cref="IImplementationStore"/>s.
    /// </summary>
    /// <param name="innerStores">
    ///   A priority-sorted list of <see cref="IImplementationStore"/>s.
    ///   Queried last-to-first for adding new <see cref="Implementation"/>s, first-to-last otherwise.
    /// </param>
    public CompositeImplementationStore(IEnumerable<IImplementationStore> innerStores)
    {
        #region Sanity checks
        if (innerStores == null) throw new ArgumentNullException(nameof(innerStores));
        #endregion

        _innerStores = innerStores.ToArray();
    }

    /// <inheritdoc/>
    public ImplementationStoreKind Kind => ImplementationStoreKind.ReadWrite;

    /// <inheritdoc />
    public void Add(ManifestDigest manifestDigest, Action<IBuilder> build)
    {
        #region Sanity checks
        if (build == null) throw new ArgumentNullException(nameof(build));
        #endregion

        if (Contains(manifestDigest)) throw new ImplementationAlreadyInStoreException(manifestDigest);

        // Find the last store the implementation can be added to (some might be write-protected)
        Exception? innerException = null;
        foreach (var store in _innerStores.Reverse())
        {
            try
            {
                // Try to add implementation to this store
                store.Add(manifestDigest, build);
                return;
            }
            #region Error handling
            catch (IOException ex)
            {
                innerException = ex; // Remember the last error
            }
            catch (UnauthorizedAccessException ex)
            {
                innerException = ex; // Remember the last error
            }
#if NETFRAMEWORK
            catch (RemotingException ex)
            {
                innerException = ex; // Remember the last error
            }
#endif
            #endregion
        }

        // If we reach this, the implementation could not be added to any store
        innerException?.Rethrow();
    }

    /// <inheritdoc/>
    public string Path
        => string.Join(System.IO.Path.PathSeparator.ToString(), Stores.Select(x => x.Path));

    /// <inheritdoc/>
    public bool Contains(ManifestDigest manifestDigest)
        => _innerStores.Any(x => x.Contains(manifestDigest));

    /// <inheritdoc/>
    public string? GetPath(ManifestDigest manifestDigest)
        => _innerStores.TrySelect(store => store.GetPath(manifestDigest), (UnauthorizedAccessException _) => {})
                       .WhereNotNull()
                       .FirstOrDefault();

    /// <inheritdoc/>
    public IEnumerable<ManifestDigest> ListAll()
        // Merge the lists from all contained stores, eliminating duplicates
        => _innerStores.TrySelect(x => x.ListAll(), (UnauthorizedAccessException _) => {})
                       .SelectMany(x => x)
                       .Distinct();

    /// <inheritdoc/>
    public IEnumerable<string> ListAllTemp()
        // Merge the lists from all contained stores, eliminating duplicates
        => _innerStores.TrySelect(x => x.ListAllTemp(), (UnauthorizedAccessException _) => {})
                       .SelectMany(x => x)
                       .Distinct(StringComparer.Ordinal);

    /// <inheritdoc />
    public void Verify(ManifestDigest manifestDigest)
    {
        Exception? lastException = null;
        foreach (var store in _innerStores.Where(x => x.Kind != ImplementationStoreKind.Service))
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
        foreach (var store in _innerStores.Reverse())
            removed |= store.Remove(manifestDigest);

        return removed;
    }

    /// <inheritdoc />
    public void Purge()
    {
        foreach (var store in _innerStores.Reverse())
            store.Purge();
    }

    /// <inheritdoc/>
    public long Optimise()
    {
        // Try to optimize all contained stores
        return _innerStores.Reverse().Sum(x => x.Optimise());
    }

    /// <summary>
    /// Creates string representation suitable for console output.
    /// </summary>
    public override string ToString()
        => "CompositeStore: " + string.Join(", ", _innerStores.Select(x => x.ToString()).WhereNotNull());
}
