// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Accepts implementations and stores them.
/// </summary>
public interface IImplementationSink
{
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
    /// Adds a new implementation.
    /// </summary>
    /// <param name="manifestDigest">The digest the implementation is supposed to match.</param>
    /// <param name="build">Callback for building the implementation.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="ImplementationAlreadyInStoreException">There is already an implementation with the specified <paramref name="manifestDigest"/> in the store.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    /// <exception cref="DigestMismatchException">The implementation's content doesn't match the <paramref name="manifestDigest"/>.</exception>
    void Add(ManifestDigest manifestDigest, [InstantHandle] Action<IBuilder> build);
}
