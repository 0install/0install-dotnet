// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using NanoByte.Common.Undo;
using ZeroInstall.Model;
using ZeroInstall.Publish.Properties;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Implementations.Build;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// Helper methods for manipulating <see cref="RetrievalMethod"/>s.
    /// </summary>
    public static class RetrievalMethodUtils
    {
        #region Download
        /// <summary>
        /// Downloads and applies a <see cref="RetrievalMethod"/> and adds missing properties.
        /// </summary>
        /// <param name="retrievalMethod">The <see cref="RetrievalMethod"/> to be downloaded.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion.</param>
        /// <returns>A temporary directory containing the extracted content.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="UriFormatException"><see cref="DownloadRetrievalMethod.Href"/> inside <paramref name="retrievalMethod"/> is a relative URI that cannot be resolved.</exception>
        /// <exception cref="IOException">There is a problem access a temporary file.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a temporary file is not permitted.</exception>
        /// <exception cref="NotSupportedException">A <see cref="Archive.MimeType"/> is not supported.</exception>
        public static TemporaryDirectory DownloadAndApply(this RetrievalMethod retrievalMethod, ITaskHandler handler, ICommandExecutor? executor = null)
        {
            #region Sanity checks
            if (retrievalMethod == null) throw new ArgumentNullException(nameof(retrievalMethod));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            return retrievalMethod switch
            {
                DownloadRetrievalMethod download => download.DownloadAndApply(handler, executor),
                Recipe recipe => recipe.DownloadAndApply(handler, executor),
                _ => throw new NotSupportedException(Resources.UnknownRetrievalMethodType)
            };
        }

        /// <summary>
        /// Downloads and applies a <see cref="DownloadRetrievalMethod"/> and adds missing properties.
        /// </summary>
        /// <param name="retrievalMethod">The <see cref="DownloadRetrievalMethod"/> to be downloaded.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion.</param>
        /// <returns>A temporary directory containing the extracted content.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="UriFormatException"><see cref="DownloadRetrievalMethod.Href"/> inside <paramref name="retrievalMethod"/> is a relative URI that cannot be resolved.</exception>
        /// <exception cref="IOException">There is a problem access a temporary file.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a temporary file is not permitted.</exception>
        /// <exception cref="NotSupportedException">A <see cref="Archive.MimeType"/> is not supported.</exception>
        public static TemporaryDirectory DownloadAndApply(this DownloadRetrievalMethod retrievalMethod, ITaskHandler handler, ICommandExecutor? executor = null)
            => new Recipe {Steps = {retrievalMethod}}.DownloadAndApply(handler, executor);

        /// <summary>
        /// Downloads and applies a <see cref="Recipe"/> and adds missing properties.
        /// </summary>
        /// <param name="recipe">The <see cref="Recipe"/> to be applied.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion.</param>
        /// <returns>A temporary directory containing the result of the recipe.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="UriFormatException"><see cref="DownloadRetrievalMethod.Href"/> inside <paramref name="recipe"/> is a relative URI that cannot be resolved.</exception>
        /// <exception cref="IOException">There is a problem access a temporary file.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a temporary file is not permitted.</exception>
        /// <exception cref="NotSupportedException">A <see cref="Archive.MimeType"/> is not supported.</exception>
        public static TemporaryDirectory DownloadAndApply(this Recipe recipe, ITaskHandler handler, ICommandExecutor? executor = null)
        {
            #region Sanity checks
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            var workingDir = new TemporaryDirectory("0publish");
            var downloadedFiles = new List<TemporaryFile>();
            try
            {
                var sources = recipe.GetImplementationSources(
                    download: retrievalMethod =>
                    {
                        var file = Download(retrievalMethod, handler, executor);
                        downloadedFiles.Add(file);
                        return file;
                    },
                    implementationLookup: ImplementationStores.Default().GetPath);

                foreach (var source in sources)
                {
                    var task = source.GetApplyTask(workingDir);
                    using (task as IDisposable)
                        handler.RunTask(task);
                }

            }
            finally
            {
                foreach (var downloadedFile in downloadedFiles)
                    downloadedFile.Dispose();
            }
            return workingDir;
        }

        /// <summary>
        /// Downloads a <see cref="DownloadRetrievalMethod"/> and adds missing properties.
        /// </summary>
        /// <param name="retrievalMethod">The <see cref="DownloadRetrievalMethod"/> to be downloaded.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion.</param>
        /// <returns>A downloaded file.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="UriFormatException"><see cref="DownloadRetrievalMethod.Href"/> inside <paramref name="retrievalMethod"/> is a relative URI that cannot be resolved.</exception>
        /// <exception cref="IOException">There is a problem access a temporary file.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a temporary file is not permitted.</exception>
        public static TemporaryFile Download(this DownloadRetrievalMethod retrievalMethod, ITaskHandler handler, ICommandExecutor? executor = null)
        {
            #region Sanity checks
            if (retrievalMethod == null) throw new ArgumentNullException(nameof(retrievalMethod));
            if (retrievalMethod.Href == null) throw new ArgumentException("Missing href.", nameof(retrievalMethod));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            executor ??= new SimpleCommandExecutor();

            switch (retrievalMethod)
            {
                case Archive archive:
                    // Guess MIME types now because the file ending is not known later
                    if (string.IsNullOrEmpty(archive.MimeType))
                    {
                        string mimeType = Archive.GuessMimeType(archive.Href!.OriginalString);
                        executor.Execute(SetValueCommand.For(() => archive.MimeType, newValue: mimeType));
                    }
                    break;

                case SingleFile file:
                    // Guess file name based on URL
                    if (string.IsNullOrEmpty(file.Destination))
                    {
                        string destination = file.Href!.GetLocalFileName();
                        executor.Execute(SetValueCommand.For(() => file.Destination, newValue: destination));
                    }
                    break;

                default:
                    throw new NotSupportedException($"Unknown retrieval method: ${retrievalMethod}");
            }

            // Download the file
            var href = ModelUtils.GetAbsoluteHref(retrievalMethod.Href, executor.Path);
            var downloadedFile = new TemporaryFile("0publish");
            handler.RunTask(new DownloadFile(href, downloadedFile)); // Defer task to handler

            UpdateSize(retrievalMethod, downloadedFile, executor);

            return downloadedFile;
        }
        #endregion

        #region Local
        /// <summary>
        /// Applies a locally stored <see cref="DownloadRetrievalMethod"/> and adds missing properties.
        /// </summary>
        /// <param name="retrievalMethod">The <see cref="DownloadRetrievalMethod"/> to be applied.</param>
        /// <param name="localPath">The local file path where the <paramref name="retrievalMethod"/> is located.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion.</param>
        /// <returns>A temporary directory containing the extracted content.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">There is a problem access a temporary file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a temporary file is not permitted.</exception>
        public static TemporaryDirectory LocalApply(this DownloadRetrievalMethod retrievalMethod, string localPath, ITaskHandler handler, ICommandExecutor? executor = null)
        {
            #region Sanity checks
            if (retrievalMethod == null) throw new ArgumentNullException(nameof(retrievalMethod));
            if (string.IsNullOrEmpty(localPath)) throw new ArgumentNullException(nameof(localPath));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            executor ??= new SimpleCommandExecutor();

            UpdateSize(retrievalMethod, localPath, executor);

            var extractionDir = new TemporaryDirectory("0publish");
            try
            {
                switch (retrievalMethod)
                {
                    case Archive archive:
                        // Guess MIME types now because the file ending is not known later
                        if (string.IsNullOrEmpty(archive.MimeType))
                        {
                            string mimeType = Archive.GuessMimeType(localPath);
                            executor.Execute(SetValueCommand.For(() => archive.MimeType, newValue:  mimeType));
                        }
                        break;

                    case SingleFile file:
                        // Guess file name based on local path
                        if (string.IsNullOrEmpty(file.Destination))
                        {
                            string destination = Path.GetFileName(localPath);
                            executor.Execute(SetValueCommand.For(() => file.Destination, newValue: destination));
                        }
                        break;
                }

                var task = retrievalMethod.GetImplementationSource(localPath).GetApplyTask(extractionDir);
                using (task as IDisposable)
                    handler.RunTask(task);
            }
            #region Error handling
            catch
            {
                extractionDir.Dispose();
                throw;
            }
            #endregion

            return extractionDir;
        }
        #endregion

        #region Shared
        /// <summary>
        /// Updates <see cref="DownloadRetrievalMethod.Size"/> based on the file size of a local file.
        /// </summary>
        /// <param name="retrievalMethod">The element to update.</param>
        /// <param name="filePath">The path of the file to get the size from.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion.</param>
        private static void UpdateSize(DownloadRetrievalMethod retrievalMethod, string filePath, ICommandExecutor executor)
        {
            long size = new FileInfo(filePath).Length;

            if (retrievalMethod is Archive archive)
                size -= archive.StartOffset;

            if (retrievalMethod.Size != size)
                executor.Execute(SetValueCommand.For(() => retrievalMethod.Size, newValue: size));
        }
        #endregion
    }
}
