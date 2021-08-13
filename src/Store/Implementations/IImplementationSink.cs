// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using JetBrains.Annotations;
using ZeroInstall.Model;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Accepts implementations and stores them.
    /// </summary>
    public interface IImplementationSink
    {
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
}
