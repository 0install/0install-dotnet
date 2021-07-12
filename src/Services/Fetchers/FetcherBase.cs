// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Services.Native;
using ZeroInstall.Services.Properties;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Implementations.Archives;
using ZeroInstall.Store.Implementations.Build;

namespace ZeroInstall.Services.Fetchers
{
    /// <summary>
    /// Base class for <see cref="IFetcher"/> implementations using template methods.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public abstract class FetcherBase : IFetcher
    {
        private readonly IImplementationStore _implementationStore;

        /// <summary>A callback object used when the the user needs to be informed about progress.</summary>
        protected readonly ITaskHandler Handler;

        /// <summary>
        /// Creates a new download fetcher.
        /// </summary>
        /// <param name="implementationStore">The location to store the downloaded and unpacked <see cref="Implementation"/>s in.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about progress.</param>
        protected FetcherBase(IImplementationStore implementationStore, ITaskHandler handler)
        {
            _implementationStore = implementationStore ?? throw new ArgumentNullException(nameof(implementationStore));
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <inheritdoc/>
        public abstract string? Fetch(Implementation implementation);

        /// <summary>
        /// Determines the local path of an implementation.
        /// </summary>
        /// <param name="implementation">The implementation to be located.</param>
        /// <returns>A fully qualified path to the directory containing the implementation; <c>null</c> if the requested implementation could not be found in the store or is a package implementation.</returns>
        protected string? GetPathSafe(ImplementationBase implementation)
            => implementation.ID.StartsWith(ExternalImplementation.PackagePrefix)
                ? null
                : _implementationStore.GetPath(implementation.ManifestDigest);

        /// <summary>
        /// Executes the best possible <see cref="RetrievalMethod"/> for an <see cref="Implementation"/>.
        /// </summary>
        /// <param name="implementation">The implementation to be retrieved.</param>
        /// <remarks>Make sure <see cref="Implementation.RetrievalMethods"/> is not empty before calling this!</remarks>
        /// <exception cref="OperationCanceledException">A download or IO task was canceled from another thread.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="NotSupportedException">A file format, protocol, etc. is unknown or not supported.</exception>
        /// <exception cref="IOException">A downloaded file could not be written to the disk or extracted.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to <see cref="IImplementationStore"/> is not permitted.</exception>
        /// <exception cref="DigestMismatchException">An <see cref="Implementation"/>'s <see cref="Archive"/>s don't match the associated <see cref="ManifestDigest"/>.</exception>
        protected void Retrieve(Implementation implementation)
            => implementation
              .RetrievalMethods
              .OrderBy(x => x, RetrievalMethodRanker.Instance)
              .TryAny(retrievalMethod =>
               {
                   if (retrievalMethod is ExternalRetrievalMethod externalRetrievalMethod)
                       Retrieve(externalRetrievalMethod);
                   else
                   {
                       var digest = implementation.ManifestDigest;
                       if (digest.Best == null) throw new NotSupportedException(string.Format(Resources.NoManifestDigest, implementation.ID));

                       Handler.CancellationToken.ThrowIfCancellationRequested();
                       Retrieve(retrievalMethod, implementation.ManifestDigest);
                   }
               });

        /// <summary>
        /// Handles the execution of <see cref="ExternalRetrievalMethod.Install"/>.
        /// </summary>
        private void Retrieve(ExternalRetrievalMethod externalRetrievalMethod)
        {
            if (externalRetrievalMethod.Install == null) throw new NotSupportedException("No installation callback registered for native package.");

            if (!string.IsNullOrEmpty(externalRetrievalMethod.ConfirmationQuestion))
            {
                if (!Handler.Ask(externalRetrievalMethod.ConfirmationQuestion,
                    defaultAnswer: false, alternateMessage: externalRetrievalMethod.ConfirmationQuestion)) throw new OperationCanceledException();
            }

            externalRetrievalMethod.Install();
        }

        /// <summary>
        /// Executes a specific <see cref="RetrievalMethod"/>.
        /// </summary>
        /// <param name="retrievalMethod">The retrieval method to execute.</param>
        /// <param name="manifestDigest">The digest the result of the retrieval method should produce.</param>
        /// <exception cref="OperationCanceledException">A download or IO task was canceled from another thread.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="NotSupportedException">A file format, protocol, etc. is unknown or not supported.</exception>
        /// <exception cref="IOException">A downloaded file could not be written to the disk or extracted.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to <see cref="IImplementationStore"/> is not permitted.</exception>
        /// <exception cref="DigestMismatchException">An <see cref="Implementation"/>'s <see cref="Archive"/>s don't match the associated <see cref="ManifestDigest"/>.</exception>
        private void Retrieve(RetrievalMethod retrievalMethod, ManifestDigest manifestDigest)
        {
            var recipe = retrievalMethod as Recipe ?? new Recipe {Steps = {(IRecipeStep)retrievalMethod}};

            // Fail fast on unsupported Archive types
            foreach (var archive in recipe.Steps.OfType<Archive>())
            {
                if (string.IsNullOrEmpty(archive.MimeType))
                    throw new InvalidOperationException($"Archive is missing MIME type. {nameof(Archive.Normalize)}() has no not been called.");
                if (!ArchiveExtractor.IsSupported(archive.MimeType))
                    throw new NotSupportedException($"Archive {archive.Href} of type {archive.MimeType} not supported."); // TODO: Localize
            }

            var downloadedFiles = new List<TemporaryFile>();
            try
            {
                var sources = recipe.GetImplementationSources(
                    download: downloadRetrievalMethod =>
                    {
                        var file = Download(downloadRetrievalMethod);
                        downloadedFiles.Add(file);
                        return file;
                    },
                    implementationLookup: Fetch);

                _implementationStore.Add(manifestDigest, sources);
            }
            #region Error handling
            catch (ImplementationAlreadyInStoreException)
            {}
            catch (DigestMismatchException)
            {
                Log.Error("Damaged download: " + retrievalMethod);
                throw;
            }
            #endregion
            finally
            {
                foreach (var downloadedFile in downloadedFiles)
                    downloadedFile.Dispose();
            }
        }

        /// <summary>
        /// Downloads a <see cref="DownloadRetrievalMethod"/> to a temporary file.
        /// </summary>
        /// <param name="retrievalMethod">The file to download.</param>
        /// <param name="tag">The <see cref="ITask.Tag"/> to use for correlation.</param>
        /// <returns>The downloaded temporary file.</returns>
        /// <exception cref="OperationCanceledException">A download was canceled from another thread.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="IOException">A downloaded file could not be written to the disk or.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to <see cref="IImplementationStore"/> is not permitted.</exception>
        protected virtual TemporaryFile Download(DownloadRetrievalMethod retrievalMethod, string? tag = null)
        {
            #region Sanity checks
            if (retrievalMethod == null) throw new ArgumentNullException(nameof(retrievalMethod));
            if (retrievalMethod.Href == null) throw new ArgumentException("Missing href.", nameof(retrievalMethod));
            #endregion

            retrievalMethod.Validate();

            var tempFile = new TemporaryFile("0install-fetcher");
            try
            {
                Handler.RunTask(new DownloadFile(retrievalMethod.Href!, tempFile, retrievalMethod.DownloadSize) {Tag = tag});
                return tempFile;
            }
            #region Error handling
            catch
            {
                tempFile.Dispose();
                throw;
            }
            #endregion
        }
    }
}
