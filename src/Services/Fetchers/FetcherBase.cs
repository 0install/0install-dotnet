// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Tasks;
using ZeroInstall.Archives;
using ZeroInstall.Archives.Extractors;
using ZeroInstall.Model;
using ZeroInstall.Services.Native;
using ZeroInstall.Services.Properties;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;

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
                   Handler.CancellationToken.ThrowIfCancellationRequested();

                   ManifestDigest GetDigest()
                   {
                       if (implementation.ManifestDigest.Best == null) throw new NotSupportedException(string.Format(Resources.NoManifestDigest, implementation.ID));
                       return implementation.ManifestDigest;
                   }

                   try
                   {
                       switch (retrievalMethod)
                       {
                           case ExternalRetrievalMethod external:
                               Retrieve(external);
                               break;
                           case Recipe recipe:
                               Retrieve(recipe,  GetDigest());
                               break;
                           case IRecipeStep step:
                               Retrieve(new Recipe {Steps = {step}}, GetDigest());
                               break;
                       }
                   }
                   #region Error handling
                   catch (ImplementationAlreadyInStoreException)
                   {}
                   catch (DigestMismatchException)
                   {
                       Log.Error(string.Format(Resources.FetcherProblem, retrievalMethod));
                       throw;
                   }
                   #endregion
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
        /// <param name="recipe">The retrieval method to execute.</param>
        /// <param name="manifestDigest">The digest the result of the retrieval method should produce.</param>
        /// <exception cref="OperationCanceledException">A download or IO task was canceled from another thread.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="NotSupportedException">A file format, protocol, etc. is unknown or not supported.</exception>
        /// <exception cref="IOException">A downloaded file could not be written to the disk or extracted.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to <see cref="IImplementationStore"/> is not permitted.</exception>
        /// <exception cref="DigestMismatchException">An <see cref="Implementation"/>'s <see cref="Archive"/>s don't match the associated <see cref="ManifestDigest"/>.</exception>
        private void Retrieve(Recipe recipe, ManifestDigest manifestDigest)
        {
            CheckArchiveTypes(recipe.Steps.OfType<Archive>());

            _implementationStore.Add(manifestDigest, builder =>
            {
                foreach (var step in recipe.Steps)
                {
                    switch (step)
                    {
                        case DownloadRetrievalMethod download:
                            Download(builder, download, manifestDigest.Best);
                            break;
                        case RemoveStep remove:
                            builder.Remove(remove);
                            break;
                        case RenameStep rename:
                            builder.Rename(rename);
                            break;
                        case CopyFromStep copyFrom:
                            builder.CopyFrom(copyFrom, Fetch(copyFrom.Implementation) ?? throw new IOException($"Unable to process {copyFrom}."), Handler);
                            break;
                        default:
                            throw new NotSupportedException($"Unknown recipe step: ${step}");
                    }
                }
            });
        }

        private void CheckArchiveTypes(IEnumerable<Archive> archives)
        {
            foreach (var archive in archives)
            {
                try
                {
                    archive.MimeType ??= Archive.GuessMimeType(archive.Href.OriginalString);
                    ArchiveExtractor.For(archive.MimeType, Handler);
                }
                catch (NotSupportedException ex)
                {
                    // Wrap exception to add context information
                    throw new NotSupportedException(string.Format(Resources.FetcherProblem, archive), ex);
                }
            }
        }

        /// <summary>
        /// Downloads (part of) an implementation.
        /// </summary>
        /// <param name="builder">The builder to write the downloaded data to.</param>
        /// <param name="download">The retrieval method used to download data.</param>
        /// <param name="tag">A <see cref="ITask.Tag"/> used to group progress bars. Usually <see cref="ManifestDigest.Best"/>.</param>
        protected virtual void Download(IBuilder builder, DownloadRetrievalMethod download, object? tag)
            => Handler.RunTask(new DownloadFile(download.Href, stream => builder.Add(download, stream, Handler, tag), download.DownloadSize) {Tag = tag});
    }
}
