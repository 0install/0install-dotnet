// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Manages a directory that stores implementations. Also known as an implementation cache.
    /// </summary>
    public interface IImplementationStore : IImplementationSink
    {
        /// <summary>
        /// Indicates what kind of access to this store is possible.
        /// </summary>
        ImplementationStoreKind Kind { get; }

        /// <summary>
        /// The path to the underlying directory in the file system.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Determines whether the store contains a local copy of an implementation identified by a specific <see cref="ManifestDigest"/>.
        /// </summary>
        /// <param name="manifestDigest">The digest of the implementation to check for.</param>
        /// <returns>
        ///   <c>true</c> if the specified implementation is available in the store;
        ///   <c>false</c> if the specified implementation is not available in the store or if read access to the store is not permitted.
        /// </returns>
        /// <remarks>If read access to the store is not permitted, no exception is thrown.</remarks>
        bool Contains(ManifestDigest manifestDigest);

        /// <summary>
        /// Determines the local path of an implementation with a given <see cref="ManifestDigest"/>.
        /// </summary>
        /// <param name="manifestDigest">The digest the implementation to look for.</param>
        /// <exception cref="UnauthorizedAccessException">Read access to the store is not permitted.</exception>
        /// <returns>A fully qualified path to the directory containing the implementation; <c>null</c> if the requested implementation could not be found in the store.</returns>
        string? GetPath(ManifestDigest manifestDigest);

        /// <summary>
        /// Returns a list of all implementations currently in the store.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Read access to the store is not permitted.</exception>
        /// <returns>A list of implementations formatted as "algorithm=digest" (e.g. "sha256=123abc").</returns>
        IEnumerable<ManifestDigest> ListAll();

        /// <summary>
        /// Returns a list of temporary directories currently in the store.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Read access to the store is not permitted.</exception>
        /// <returns>A list of fully qualified paths.</returns>
        IEnumerable<string> ListAllTemp();

        /// <summary>
        /// Removes a specific implementation from the cache.
        /// </summary>
        /// <param name="manifestDigest">The digest of the implementation to be removed.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <returns><c>true</c> if the implementation was successfully removed; <c>false</c> if it could not be removed, e.g. because no implementation matching <paramref name="manifestDigest"/> was found.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">Thrown if the implementation could not be deleted.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if write access to the store is not permitted.</exception>
        bool Remove(ManifestDigest manifestDigest, ITaskHandler handler);

        /// <summary>
        /// Recalculates the digests for an entry in the store and ensures it is correct. Will delete damaged implementations after user confirmation.
        /// </summary>
        /// <param name="manifestDigest">The digest of the implementation to be verified.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress or asked questions.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="NotSupportedException"><paramref name="manifestDigest"/> does not list any supported digests.</exception>
        /// <exception cref="ImplementationNotFoundException">No implementation matching <paramref name="manifestDigest"/> could be found in the store.</exception>
        /// <exception cref="IOException">The entry's directory could not be processed.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the entry's directory is not permitted.</exception>
        /// <remarks>If the store does not support verification this method call may be silently ignored.</remarks>
        void Verify(ManifestDigest manifestDigest, ITaskHandler handler);

        /// <summary>
        /// Reads in all the manifest files in the store and looks for duplicates (files with the same permissions, modification time and digest). When it finds a pair, it deletes one and replaces it with a hard-link to the other.
        /// </summary>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <returns>The number of bytes saved by deduplication.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">Two files could not be hard-linked together.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the store is not permitted.</exception>
        /// <exception cref="DigestMismatchException">A damaged implementation is encountered while optimizing.</exception>
        /// <remarks>If the store does not support optimising this method call may be silently ignored.</remarks>
        long Optimise(ITaskHandler handler);
    }
}
