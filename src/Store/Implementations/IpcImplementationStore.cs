// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NETSTANDARD2_0
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Implementations.Archives;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Provides transparent access to an <see cref="IImplementationStore"/> running in another process (the Store Service).
    /// </summary>
    public partial class IpcImplementationStore : IImplementationStore
    {
        /// <inheritdoc/>
        public ImplementationStoreKind Kind => ImplementationStoreKind.Service;

        /// <inheritdoc/>
        public string DirectoryPath
        {
            get
            {
                try
                {
                    return GetProxy().DirectoryPath;
                }
                #region Error handling
                catch (RemotingException ex)
                {
                    Log.Debug("Unable to connect to Store Service");
                    Log.Debug(ex);
                    return null;
                }
                catch (SerializationException ex)
                {
                    Log.Debug("Incompatible version of Store Service");
                    Log.Debug(ex);
                    return null;
                }
                #endregion
            }
        }

        /// <summary>
        /// Always returns empty list. Use a non-IPC <see cref="IImplementationStore"/> for this method instead.
        /// </summary>
        /// <remarks>Using the store service for this is unnecessary since it only requires read access to the file system.</remarks>
        public IEnumerable<ManifestDigest> ListAll() => Enumerable.Empty<ManifestDigest>();

        /// <summary>
        /// Always returns empty list. Use a non-IPC <see cref="IImplementationStore"/> for this method instead.
        /// </summary>
        /// <remarks>Using the store service for this is unnecessary since it only requires read access to the file system.</remarks>
        public IEnumerable<string> ListAllTemp() => Enumerable.Empty<string>();

        /// <summary>
        /// Always returns <c>false</c>. Use a non-IPC <see cref="IImplementationStore"/> for this method instead.
        /// </summary>
        /// <remarks>Using the store service for this is unnecessary since it only requires read access to the file system.</remarks>
        public bool Contains(ManifestDigest manifestDigest) => false;

        /// <summary>
        /// Always returns <c>false</c>. Use a non-IPC <see cref="IImplementationStore"/> for this method instead.
        /// </summary>
        /// <remarks>Using the store service for this is unnecessary since it only requires read access to the file system.</remarks>
        public bool Contains(string directory) => false;

        /// <inheritdoc/>
        public void Flush()
        {
            // No internal caching
        }

        /// <summary>
        /// Always returns <c>null</c>. Use a non-IPC <see cref="IImplementationStore"/> for this method instead.
        /// </summary>
        /// <remarks>Using the store service for this is unnecessary since it only requires read access to the file system.</remarks>
        public string GetPath(ManifestDigest manifestDigest) => null;

        /// <inheritdoc/>
        public string AddDirectory(string path, ManifestDigest manifestDigest, ITaskHandler handler)
        {
            try
            {
                string result = GetProxy().AddDirectory(path, manifestDigest, handler);
                Log.Info("Sent implementation to Store Service: " + manifestDigest.Best);
                return result;
            }
            #region Error handling
            catch (RemotingException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            catch (SerializationException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion
        }

        /// <inheritdoc/>
        public string AddArchives(IEnumerable<ArchiveFileInfo> archiveInfos, ManifestDigest manifestDigest, ITaskHandler handler)
        {
            try
            {
                string result = GetProxy().AddArchives(archiveInfos, manifestDigest, handler);
                Log.Info("Sent implementation to Store Service: " + manifestDigest.Best);
                return result;
            }
            #region Error handling
            catch (RemotingException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            catch (SerializationException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion
        }

        /// <summary>
        /// Does nothing. Should be handled by an <see cref="DiskImplementationStore"/> directly instead of using the service.
        /// </summary>
        public bool Remove(ManifestDigest manifestDigest, ITaskHandler handler) => false;

        /// <summary>
        /// Does nothing. Should be handled by an <see cref="DiskImplementationStore"/> directly instead of using the service.
        /// </summary>
        public long Optimise(ITaskHandler handler) => 0;

        /// <summary>
        /// Does nothing. Should be handled by an <see cref="DiskImplementationStore"/> directly instead of using the service.
        /// </summary>
        public void Verify(ManifestDigest manifestDigest, ITaskHandler handler) {}

        #region Conversion
        /// <summary>
        /// Returns a fixed string.
        /// </summary>
        // NOTE: Do not touch DirectoryPath here to avoid potentially expensive IPC
        public override string ToString() => "Connection to Store Service (if available)";
        #endregion
    }
}
#endif
