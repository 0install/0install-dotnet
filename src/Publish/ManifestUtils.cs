// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Publish.Properties;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// Helper methods for working with <see cref="Manifest"/>s and <see cref="ManifestDigest"/>s.
    /// </summary>
    public static class ManifestUtils
    {
        /// <summary>
        /// Calculates a <see cref="Manifest"/> digest for a directory.
        /// </summary>
        /// <param name="path">The path of the directory to calculate the digest for.</param>
        /// <param name="format">The <see cref="ManifestFormat"/> to use for generating the manifest and digest.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <returns>The digest with a format prefix.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">There is a problem access a temporary file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a temporary file is not permitted.</exception>
#if NETSTANDARD
        [System.Diagnostics.Contracts.Pure]
#endif
        public static string CalculateDigest(string path, ManifestFormat format, ITaskHandler handler)
        {
            var manifestGenerator = new ManifestGenerator(path, format);
            handler.RunTask(manifestGenerator);
            return manifestGenerator.Manifest.CalculateDigest();
        }

        /// <summary>
        /// Generates a <see cref="ManifestDigest"/> for a directory using the recommended <see cref="ManifestFormat"/>.
        /// </summary>
        /// <param name="path">The path of the directory to generate the digest for.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <returns>The generated digest.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">There is a problem access a temporary file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a temporary file is not permitted.</exception>
#if NETSTANDARD
        [System.Diagnostics.Contracts.Pure]
#endif
        public static ManifestDigest GenerateDigest(string path, ITaskHandler handler)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            var digest = new ManifestDigest(CalculateDigest(path, ManifestFormat.Sha256New, handler));

            if (digest.PartialEquals(ManifestDigest.Empty))
                Log.Warn(Resources.EmptyImplementation);

            return digest;
        }
    }
}
