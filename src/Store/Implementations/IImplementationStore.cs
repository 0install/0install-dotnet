// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Implementations;

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
    string? Path { get; }

    /// <summary>
    /// Determines the local path of an implementation with a given <see cref="ManifestDigest"/>.
    /// </summary>
    /// <param name="manifestDigest">The digest the implementation to look for.</param>
    /// <returns>A fully qualified path to the directory containing the implementation; <c>null</c> if the requested implementation could not be found in the store.</returns>
    /// <exception cref="IOException">The implementation directory is missing content and could not be deleted.</exception>
    /// <exception cref="UnauthorizedAccessException">The implementation directory is missing content and write access to the store is not permitted.</exception>
    string? GetPath(ManifestDigest manifestDigest);

    /// <summary>
    /// Returns a list of all implementations currently in the store.
    /// </summary>
    /// <exception cref="IOException">A problem occurred while reading from the store.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the store is not permitted.</exception>
    /// <returns>A list of implementations formatted as "algorithm=digest" (e.g. "sha256=123abc").</returns>
    IEnumerable<ManifestDigest> ListAll();

    /// <summary>
    /// Returns a list of temporary directories currently in the store.
    /// </summary>
    /// <exception cref="IOException">A problem occurred while reading from the store.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the store is not permitted.</exception>
    /// <returns>A list of fully qualified paths.</returns>
    IEnumerable<string> ListTemp();

    /// <summary>
    /// Checks whether an implementation in the store still matches the expected digest.
    /// Asks the user whether to delete the implementation if it does not match.
    /// </summary>
    /// <param name="manifestDigest">The digest of the implementation to be verified.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="NotSupportedException"><paramref name="manifestDigest"/> does not list any supported digests.</exception>
    /// <exception cref="IOException">The implementation's directory could not be processed.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the implementation's directory is not permitted.</exception>
    /// <exception cref="ImplementationNotFoundException">No implementation matching <paramref name="manifestDigest"/> could be found in the store.</exception>
    void Verify(ManifestDigest manifestDigest);

    /// <summary>
    /// Removes a specific implementation from the store.
    /// </summary>
    /// <param name="manifestDigest">The digest of the implementation to be removed.</param>
    /// <returns><c>true</c> if the implementation was successfully removed; <c>false</c> if it could not be removed, e.g., because it does not exist or is locked.</returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="NotAdminException">Needs admin rights to delete from this store.</exception>
    bool Remove(ManifestDigest manifestDigest);

    /// <summary>
    /// Removes a specific temporary directory from the store.
    /// </summary>
    /// <param name="path">The fully qualified path of the directory.</param>
    /// <returns><c>true</c> if the directory was successfully removed; <c>false</c> if it could not be removed, e.g. because it does not exist inside the store.</returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">The directory could not be deleted.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the store is not permitted.</exception>
    bool RemoveTemp(string path);

    /// <summary>
    /// Removes all implementations and temporary directories from a store.
    /// </summary>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">An implementation could not be deleted.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the store is not permitted.</exception>
    void Purge();

    /// <summary>
    /// Reads in all the manifest files in the store and looks for duplicates (files with the same permissions, modification time and digest). When it finds a pair, it deletes one and replaces it with a hard-link to the other.
    /// </summary>
    /// <returns>The number of bytes saved by deduplication.</returns>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">Two files could not be hard-linked together.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the store is not permitted.</exception>
    /// <exception cref="DigestMismatchException">A damaged implementation is encountered while optimizing.</exception>
    /// <remarks>If the store does not support optimising this method call may be silently ignored.</remarks>
    long Optimise();
}
