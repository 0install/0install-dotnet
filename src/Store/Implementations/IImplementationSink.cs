// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations.Build;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Accepts implementations and stores them.
    /// </summary>
    public interface IImplementationSink
    {
        /// <summary>
        /// Executes one or more steps to build an implementation and stores it.
        /// </summary>
        /// <param name="manifestDigest">The digest the implementation is supposed to match.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="sources">The sources providing content for the implementation.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="NotSupportedException">One of the <paramref name="sources"/> types or the <see cref="ManifestDigest"/> format is unknown or not supported.</exception>
        /// <exception cref="IOException">One of the <paramref name="sources"/> could not be applied.</exception>
        /// <exception cref="ImplementationAlreadyInStoreException">There is already an <see cref="Implementation"/> with the specified <paramref name="manifestDigest"/> in the store.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access is not permitted.</exception>
        /// <exception cref="DigestMismatchException">The <paramref name="sources"/>' content doesn't match the <paramref name="manifestDigest"/>.</exception>
        void Add(ManifestDigest manifestDigest, ITaskHandler handler, params IImplementationSource[] sources);
    }
}
