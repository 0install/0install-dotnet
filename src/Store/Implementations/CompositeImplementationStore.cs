// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations.Archives;

#if NETFRAMEWORK
using System.Runtime.Remoting;
#endif

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Combines multiple <see cref="IImplementationStore"/>s as a composite. Adds memory caching for <see cref="IImplementationStore.Contains(ManifestDigest)"/>.
    /// </summary>
    /// <remarks>
    ///   <para>When adding new <see cref="Implementation"/>s the last child <see cref="IImplementationStore"/> that doesn't throw an <see cref="UnauthorizedAccessException"/> is used.</para>
    ///   <para>When when retrieving existing <see cref="Implementation"/>s the first child <see cref="IImplementationStore"/> that returns <c>true</c> for <see cref="IImplementationStore.Contains(ManifestDigest)"/> is used.</para>
    /// </remarks>
    public class CompositeImplementationStore : MarshalByRefObject, IImplementationStore
    {
        #region Properties
        private readonly IImplementationStore[] _innerStores;

        /// <summary>
        /// The <see cref="IImplementationStore"/>s this store is internally composed of.
        /// </summary>
        public IEnumerable<IImplementationStore> Stores => new ReadOnlyCollection<IImplementationStore>(_innerStores);

        /// <inheritdoc/>
        public ImplementationStoreKind Kind => ImplementationStoreKind.ReadWrite;

        /// <inheritdoc/>
        public string Path => string.Join(System.IO.Path.PathSeparator.ToString(), Stores.Select(x => x.Path));
        #endregion

        #region Constructor
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
            _containsCache = new(manifestDigest => _innerStores.Any(x => x.Contains(manifestDigest)));
        }
        #endregion

        //--------------------//

        #region List all
        /// <inheritdoc/>
        public IEnumerable<ManifestDigest> ListAll()
            // Merge the lists from all contained stores, eliminating duplicates
            => new HashSet<ManifestDigest>(_innerStores.SelectMany(x => x.ListAllSafe()));

        /// <inheritdoc/>
        public IEnumerable<string> ListAllTemp()
            // Merge the lists from all contained stores, eliminating duplicates
            => new HashSet<string>(_innerStores.SelectMany(x => x.ListAllTempSafe()), StringComparer.Ordinal);
        #endregion

        #region Contains
        private readonly TransparentCache<ManifestDigest, bool> _containsCache;

        /// <inheritdoc/>
        public bool Contains(ManifestDigest manifestDigest) => _containsCache[manifestDigest];

        /// <inheritdoc/>
        public bool Contains(string directory) => _innerStores.Any(store => store.Contains(directory));

        /// <inheritdoc/>
        public void Flush() => _containsCache.Clear();
        #endregion

        #region Get path
        /// <inheritdoc/>
        public string? GetPath(ManifestDigest manifestDigest)
            // Use the first store that contains the implementation
            => _innerStores.Select(store => store.GetPathSafe(manifestDigest))
                      .WhereNotNull()
                      .FirstOrDefault();
        #endregion

        #region Add directory
        /// <inheritdoc/>
        public string AddDirectory(string path, ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            Flush();

            if (Contains(manifestDigest)) throw new ImplementationAlreadyInStoreException(manifestDigest);

            // Find the last store the implementation can be added to (some might be write-protected)
            Exception? innerException = null;
            foreach (var store in _innerStores.Reverse())
            {
                try
                {
                    // Try to add implementation to this store
                    return store.AddDirectory(path, manifestDigest, handler);
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
            throw innerException?.Rethrow() ?? new InvalidOperationException();
        }
        #endregion

        #region Add archive
        /// <inheritdoc/>
        public string AddArchives(IEnumerable<ArchiveFileInfo> archiveInfos, ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (archiveInfos == null) throw new ArgumentNullException(nameof(archiveInfos));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            Flush();

            if (Contains(manifestDigest)) throw new ImplementationAlreadyInStoreException(manifestDigest);

            // Find the last store the implementation can be added to (some might be write-protected)
            Exception? innerException = null;
            archiveInfos = archiveInfos.ToArray();
            foreach (var store in _innerStores.Reverse())
            {
                try
                {
                    // Try to add implementation to this store
                    return store.AddArchives(archiveInfos, manifestDigest, handler);
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

            // If we reach this, the implementation couldn't be added to any store
            throw innerException?.Rethrow() ?? new InvalidOperationException();
        }
        #endregion

        #region Remove
        /// <inheritdoc/>
        public bool Remove(ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            Flush();

            // Remove from _every_ store that contains the implementation
            bool removed = false;
            foreach (var store in _innerStores.Reverse())
                removed |= store.Remove(manifestDigest, handler);

            return removed;
        }
        #endregion

        #region Optimise
        /// <inheritdoc/>
        public long Optimise(ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            // Try to optimize all contained stores
            return _innerStores.Reverse().Sum(x => x.Optimise(handler));
        }
        #endregion

        #region Verify
        /// <inheritdoc/>
        public void Verify(ManifestDigest manifestDigest, ITaskHandler handler)
        {
            #region Sanity checks
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            // Verify in every store that contains the implementation
            bool verified = false;
            foreach (var store in _innerStores.Where(store => store.Contains(manifestDigest)))
            {
                store.Verify(manifestDigest, handler);
                verified = true;
            }
            if (!verified) throw new ImplementationNotFoundException(manifestDigest);
        }
        #endregion

        //--------------------//

        #region Conversion
        /// <summary>
        /// Returns the names of the child stores. Not safe for parsing!
        /// </summary>
        public override string ToString()
            => "CompositeStore: " + StringUtils.Join(", ", _innerStores.Select(x => x.ToString()!));
        #endregion
    }
}
