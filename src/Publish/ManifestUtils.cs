/*
 * Copyright 2010-2017 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using ZeroInstall.Publish.Properties;
using ZeroInstall.Store.Implementations.Manifests;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// Helper methods for working with <see cref="Manifest"/>s and <see cref="ManifestDigest"/>s.
    /// </summary>
    public class ManifestUtils
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
        [Pure, NotNull]
        public static string CalculateDigest([NotNull] string path, [NotNull] ManifestFormat format, [NotNull] ITaskHandler handler)
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
        [Pure]
        public static ManifestDigest GenerateDigest([NotNull] string path, [NotNull] ITaskHandler handler)
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
