// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Implementations.Archives;
using ZeroInstall.Store.Model;

#if !NETSTANDARD2_0
using System.Runtime.Remoting;
#endif

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Combines multiple <see cref="IStore"/>s as a composite. Adds memory caching for <see cref="IStore.Contains(ManifestDigest)"/>.
    /// </summary>
    /// <remarks>
    ///   <para>When adding new <see cref="Implementation"/>s the last child <see cref="IStore"/> that doesn't throw an <see cref="UnauthorizedAccessException"/> is used.</para>
    ///   <para>When when retrieving existing <see cref="Implementation"/>s the first child <see cref="IStore"/> that returns <c>true</c> for <see cref="IStore.Contains(ZeroInstall.Store.Model.ManifestDigest)"/> is used.</para>
    /// </remarks>
    public class CompositeStore : MarshalByRefObject, IStore
    {
        #region Properties
        private readonly IStore[] _stores;

        /// <summary>
        /// The <see cref="IStore"/>s this store is internally composed of.
        /// </summary>
        public IEnumerable<IStore> Stores => new ReadOnlyCollection<IStore>(_stores);

        /// <inheritdoc/>
        public StoreKind Kind => StoreKind.ReadWrite;

        /// <inheritdoc/>
        public string DirectoryPath => null;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new composite implementation provider with a set of <see cref="IStore"/>s.
        /// </summary>
        /// <param name="stores">
        ///   A priority-sorted list of <see cref="IStore"/>s.
        ///   Queried last-to-first for adding new <see cref="Implementation"/>s, first-to-last otherwise.
        /// </param>
        public CompositeStore([NotNull, ItemNotNull] IEnumerable<IStore> stores)
        {
            #region Sanity checks
            if (stores == null) throw new ArgumentNullException(nameof(stores));
            #endregion

            _stores = stores.ToArray();
            _containsCache = new TransparentCache<ManifestDigest, bool>(manifestDigest => _stores.Any(store => store.Contains(manifestDigest)));
        }
        #endregion

        //--------------------//

        #region List all
        /// <inheritdoc/>
        public IEnumerable<ManifestDigest> ListAll()
            // Merge the lists from all contained stores, eliminating duplicates
            => new HashSet<ManifestDigest>(_stores.SelectMany(x => x.ListAllSafe()));

        /// <inheritdoc/>
        public IEnumerable<string> ListAllTemp()
            // Merge the lists from all contained stores, eliminating duplicates
            => new HashSet<string>(_stores.SelectMany(x => x.ListAllTempSafe()), StringComparer.Ordinal);
        #endregion

        #region Contains
        private readonly TransparentCache<ManifestDigest, bool> _containsCache;

        /// <inheritdoc/>
        public bool Contains(ManifestDigest manifestDigest) => _containsCache[manifestDigest];

        /// <inheritdoc/>
        public bool Contains(string directory) => _stores.Any(store => store.Contains(directory));

        /// <inheritdoc/>
        public void Flush() => _containsCache.Clear();
        #endregion

        #region Get path
        /// <inheritdoc/>
        public string GetPath(ManifestDigest manifestDigest)
            // Use the first store that contains the implementation
            => _stores.Select(store => store.GetPathSafe(manifestDigest))
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
            Exception innerException = null;
            foreach (var store in _stores.Reverse())
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
#if !NETSTANDARD2_0
                catch (RemotingException ex)
                {
                    innerException = ex; // Remember the last error
                }
#endif
                #endregion
            }

            // If we reach this, the implementation could not be added to any store
            throw innerException?.PreserveStack() ?? new InvalidOperationException();
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
            Exception innerException = null;
            foreach (var store in _stores.Reverse())
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
#if !NETSTANDARD2_0
                catch (RemotingException ex)
                {
                    innerException = ex; // Remember the last error
                }
#endif
                #endregion
            }

            // If we reach this, the implementation couldn't be added to any store
            throw innerException?.PreserveStack() ?? new InvalidOperationException();
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
            foreach (var store in _stores.Reverse())
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
            return _stores.Reverse().Sum(x => x.Optimise(handler));
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
            foreach (var store in _stores.Where(store => store.Contains(manifestDigest)))
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
        public override string ToString() => "CompositeStore: " + StringUtils.Join(", ", _stores.Select(x => x.ToString()));
        #endregion
    }
}
