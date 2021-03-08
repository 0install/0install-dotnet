// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations.Manifests;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Helper methods for <see cref="IImplementationStore"/>s and paths.
    /// </summary>
    public static class ImplementationStoreUtils
    {
        /// <summary>
        /// Determines whether a path looks like it is inside a store implementation known by <see cref="ManifestFormat"/>.
        /// </summary>
        /// <param name="path">A path to a directory that may or may not be inside a store implementation.</param>
        /// <returns>The top-level of the detected store implementation directory if any; <c>null</c> otherwise.</returns>
        /// <remarks>Performs no file system access. Only looks at the path string itself.</remarks>
        public static string? DetectImplementationPath(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            var parts = Path.GetFullPath(path).Split(Path.DirectorySeparatorChar);
            var builder = new StringBuilder();
            foreach (string part in parts)
            {
                builder.Append(part);
                if (ManifestFormat.All.Any(format => part.StartsWith(format.Prefix + format.Separator)))
                    return builder.ToString();
                builder.Append(Path.DirectorySeparatorChar);
            }

            return null;
        }

        /// <summary>
        /// Wrapper for <see cref="IImplementationStore.ListAll"/>, handling exceptions.
        /// </summary>
        public static IEnumerable<ManifestDigest> ListAllSafe(this IImplementationStore implementationStore)
        {
            #region Sanity checks
            if (implementationStore == null) throw new ArgumentNullException(nameof(implementationStore));
            #endregion

            try
            {
                return implementationStore.ListAll();
            }
            #region Error handling
            catch (UnauthorizedAccessException)
            {
                // Ignore authorization errors since listing is not a critical task
                return Enumerable.Empty<ManifestDigest>();
            }
            #endregion
        }

        /// <summary>
        /// Wrapper for <see cref="IImplementationStore.ListAllTemp"/>, handling exceptions.
        /// </summary>
        public static IEnumerable<string> ListAllTempSafe(this IImplementationStore implementationStore)
        {
            #region Sanity checks
            if (implementationStore == null) throw new ArgumentNullException(nameof(implementationStore));
            #endregion

            try
            {
                return implementationStore.ListAllTemp();
            }
            #region Error handling
            catch (UnauthorizedAccessException)
            {
                // Ignore authorization errors since listing is not a critical task
                return Enumerable.Empty<string>();
            }
            #endregion
        }

        /// <summary>
        /// Wrapper for <see cref="IImplementationStore.GetPath"/>, handling exceptions.
        /// </summary>
        public static string? GetPathSafe(this IImplementationStore implementationStore, ManifestDigest manifestDigest)
        {
            #region Sanity checks
            if (implementationStore == null) throw new ArgumentNullException(nameof(implementationStore));
            #endregion

            try
            {
                return implementationStore.GetPath(manifestDigest);
            }
            #region Error handling
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            #endregion
        }

        /// <summary>
        /// Determines the local path of an implementation.
        /// </summary>
        /// <param name="implementationStore">The store to get the implementation from.</param>
        /// <param name="implementation">The implementation to be located.</param>
        /// <returns>A fully qualified path to the directory containing the implementation.</returns>
        /// <exception cref="ImplementationNotFoundException">The <paramref name="implementation"/> is not cached yet.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the store is not permitted.</exception>
        public static string GetPath(this IImplementationStore implementationStore, ImplementationBase implementation)
        {
            #region Sanity checks
            if (implementationStore == null) throw new ArgumentNullException(nameof(implementationStore));
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            #endregion

            if (string.IsNullOrEmpty(implementation.LocalPath))
            {
                string? path = implementationStore.GetPath(implementation.ManifestDigest);
                if (path == null) throw new ImplementationNotFoundException(implementation.ManifestDigest);
                return path;
            }
            else return implementation.LocalPath;
        }

        /// <summary>
        /// Removes all implementations from a store.
        /// </summary>
        /// <param name="implementationStore">The store to be purged.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">An implementation could not be deleted.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the store is not permitted.</exception>
        public static void Purge(this IImplementationStore implementationStore, ITaskHandler handler)
        {
            #region Sanity checks
            if (implementationStore == null) throw new ArgumentNullException(nameof(implementationStore));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            handler.RunTask(ForEachTask.Create(
                name: string.Format(Resources.DeletingDirectory, implementationStore.Path),
                target: implementationStore.ListAll(),
                work: digest => implementationStore.Remove(digest, handler)));
        }
    }
}
