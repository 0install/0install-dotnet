// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using NanoByte.Common.Undo;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Implementations.Archives;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// Helper methods for manipulating <see cref="Implementation"/>s.
    /// </summary>
    public static class ImplementationUtils
    {
        #region Build
        /// <summary>
        /// Creates a new <see cref="Implementation"/> by completing a <see cref="RetrievalMethod"/> and calculating the resulting <see cref="ManifestDigest"/>.
        /// </summary>
        /// <param name="retrievalMethod">The <see cref="RetrievalMethod"/> to use.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="keepDownloads">Used to retain downloaded implementations; can be <c>null</c>.</param>
        /// <returns>A newly created <see cref="Implementation"/> containing one <see cref="Archive"/>.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="UriFormatException"><see cref="DownloadRetrievalMethod.Href"/> inside <paramref name="retrievalMethod"/> is a relative URI that cannot be resolved.</exception>
        /// <exception cref="IOException">There was a problem extracting the archive.</exception>
        /// <exception cref="WebException">There was a problem downloading the archive.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to temporary files was not permitted.</exception>
        /// <exception cref="NotSupportedException">A <see cref="Archive.MimeType"/> is not supported.</exception>
        public static Implementation Build(RetrievalMethod retrievalMethod, ITaskHandler handler, IImplementationStore? keepDownloads = null)
        {
            #region Sanity checks
            if (retrievalMethod == null) throw new ArgumentNullException(nameof(retrievalMethod));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            var implementationDir = retrievalMethod.DownloadAndApply(handler);
            try
            {
                var digest = new Implementation {RetrievalMethods = {retrievalMethod}};
                digest.UpdateDigest(implementationDir, handler, new SimpleCommandExecutor(), keepDownloads);
                return digest;
            }
            finally
            {
                implementationDir.Dispose();
            }
        }
        #endregion

        #region Add missing
        /// <summary>
        /// Adds missing data (by downloading and inferring) to an <see cref="Implementation"/>.
        /// </summary>
        /// <param name="implementation">The <see cref="Implementation"/> to add data to.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion.</param>
        /// <param name="keepDownloads">Used to retain downloaded implementations; can be <c>null</c>.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="UriFormatException"><see cref="DownloadRetrievalMethod.Href"/> inside <paramref name="implementation"/> is a relative URI that cannot be resolved.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="IOException">There is a problem access a temporary file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a temporary file is not permitted.</exception>
        /// <exception cref="DigestMismatchException">An existing digest does not match the newly calculated one.</exception>
        public static void AddMissing(this Implementation implementation, ITaskHandler handler, ICommandExecutor? executor = null, IImplementationStore? keepDownloads = null)
        {
            #region Sanity checks
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            if (executor == null) executor = new SimpleCommandExecutor();

            ConvertSha256ToSha256New(implementation, executor);
            GenerateMissingArchive(implementation, handler, executor);

            foreach (var retrievalMethod in implementation.RetrievalMethods)
            {
                if (implementation.IsManifestDigestMissing() || retrievalMethod.IsDownloadSizeMissing())
                {
                    using var tempDir = retrievalMethod.DownloadAndApply(handler, executor);
                    implementation.UpdateDigest(tempDir, handler, executor, keepDownloads);
                }
            }
        }

        private static void GenerateMissingArchive(Implementation implementation, ITaskHandler handler, ICommandExecutor executor)
        {
            var archive = implementation.RetrievalMethods.OfType<Archive>().FirstOrDefault();
            if (archive == null || !string.IsNullOrEmpty(archive.Destination) || !string.IsNullOrEmpty(archive.Extract)) return;

            if (string.IsNullOrEmpty(implementation.LocalPath)) return;
            string directoryPath = ModelUtils.GetAbsolutePath(implementation.LocalPath, executor.Path);

            if (archive.Href == null) return;
            var archiveHref = ModelUtils.GetAbsoluteHref(archive.Href, executor.Path);
            if (!archiveHref.IsFile) return;

            implementation.UpdateDigest(directoryPath, handler, executor);

            using (var generator = ArchiveGenerator.Create(
                directoryPath,
                archiveHref.LocalPath,
                archive.MimeType ?? Archive.GuessMimeType(archiveHref.LocalPath)))
                handler.RunTask(generator);

            executor.Execute(SetValueCommand.For(() => archive.Size, newValue: new FileInfo(archiveHref.LocalPath).Length));
            executor.Execute(SetValueCommand.For<string?>(() => implementation.LocalPath, newValue: null));
        }

        private static void ConvertSha256ToSha256New(Implementation implementation, ICommandExecutor executor)
        {
            if (string.IsNullOrEmpty(implementation.ManifestDigest.Sha256) || !string.IsNullOrEmpty(implementation.ManifestDigest.Sha256New)) return;

            var digest = new ManifestDigest(
                implementation.ManifestDigest.Sha1,
                implementation.ManifestDigest.Sha1New,
                implementation.ManifestDigest.Sha256,
                implementation.ManifestDigest.Sha256.Base16Decode().Base32Encode());

            executor.Execute(SetValueCommand.For(() => implementation.ManifestDigest, newValue: digest));
        }

        [SuppressMessage("Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength", Justification = "We are explicitly looking for empty strings as opposed to null strings.")]
        private static bool IsManifestDigestMissing(this Implementation implementation)
            => implementation.ManifestDigest == default ||
               // Empty strings are used in 0template to indicate that the user wishes this value to be calculated
               implementation.ManifestDigest.Sha1New == "" ||
               implementation.ManifestDigest.Sha256 == "" ||
               implementation.ManifestDigest.Sha256New == "";

        private static bool IsDownloadSizeMissing(this RetrievalMethod retrievalMethod)
            => retrievalMethod is DownloadRetrievalMethod downloadRetrievalMethod && downloadRetrievalMethod.Size == 0;
        #endregion

        #region Digest helpers
        /// <summary>
        /// Updates the <see cref="ManifestDigest"/> in an <see cref="Implementation"/>.
        /// </summary>
        /// <param name="implementation">The <see cref="Implementation"/> to update.</param>
        /// <param name="path">The path of the directory to generate the digest for.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion.</param>
        /// <param name="keepDownloads">Used to retain downloaded implementations; can be <c>null</c>.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">There is a problem access a temporary file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a temporary file is not permitted.</exception>
        /// <exception cref="DigestMismatchException">An existing digest does not match the newly calculated one.</exception>
        private static void UpdateDigest(this Implementation implementation, string path, ITaskHandler handler, ICommandExecutor executor, IImplementationStore? keepDownloads = null)
        {
            var digest = ManifestUtils.GenerateDigest(path, handler);

            if (implementation.ManifestDigest == default)
                executor.Execute(SetValueCommand.For(() => implementation.ManifestDigest, newValue: digest));
            else if (!digest.PartialEquals(implementation.ManifestDigest))
                throw new DigestMismatchException(expectedDigest: implementation.ManifestDigest.ToString(), actualDigest: digest.ToString());

            if (string.IsNullOrEmpty(implementation.ID))
            {
                string id = ManifestUtils.CalculateDigest(path, ManifestFormat.Sha1New, handler);
                executor.Execute(SetValueCommand.For(() => implementation.ID, newValue: id));
            }

            if (keepDownloads != null)
            {
                try
                {
                    keepDownloads.AddDirectory(path, digest, handler);
                }
                #region Error handling
                catch (Exception ex)
                {
                    Log.Warn(ex);
                }
                #endregion
            }
        }
        #endregion
    }
}
