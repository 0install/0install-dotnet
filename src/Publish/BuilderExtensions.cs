﻿// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using NanoByte.Common.Undo;
using ZeroInstall.Archives;
using ZeroInstall.Model;
using ZeroInstall.Publish.Properties;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// Helpers for adding <see cref="RetrievalMethod"/>s to <see cref="IBuilder"/>s and setting missing properties.
    /// </summary>
    public static class BuilderExtensions
    {
        /// <summary>
        /// Applies a retrieval method to the implementation. Sets missing properties in the process.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="retrievalMethod">The retrieval method.</param>
        /// <param name="executor">Used to modify properties in an undoable fashion.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        public static void Add(this IBuilder builder, RetrievalMethod retrievalMethod, ICommandExecutor executor, ITaskHandler handler)
        {
            #region Sanity checks
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (retrievalMethod == null) throw new ArgumentNullException(nameof(retrievalMethod));
            if (executor == null) throw new ArgumentNullException(nameof(executor));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            void Apply(IRecipeStep step)
            {
                switch (step)
                {
                    case DownloadRetrievalMethod download:
                        builder.Add(download, executor: executor, handler: handler);
                        break;
                    case RemoveStep remove:
                        builder.Remove(remove);
                        break;
                    case RenameStep rename:
                        builder.Rename(rename);
                        break;
                    case CopyFromStep copyFrom:
                        builder.CopyFrom(copyFrom, handler);
                        break;
                    default:
                        throw new NotSupportedException($"Unknown recipe step: ${step}");
                }
            }

            switch (retrievalMethod)
            {
                case DownloadRetrievalMethod download:
                    Apply(download);
                    break;
                case Recipe recipe:
                    foreach (var step in recipe.Steps) Apply(step);
                    break;
                default:
                    throw new NotSupportedException($"Unknown retrieval method: ${retrievalMethod}");
            }
        }

        /// <summary>
        /// Applies a retrieval method to the implementation. Sets missing properties in the process.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="retrievalMethod">The retrieval method.</param>
        /// <param name="executor">Used to modify properties in an undoable fashion.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        /// <param name="localPath">An optional local file path where the <paramref name="retrievalMethod"/> has already been downloaded.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="IOException">There is a problem accessing <paramref name="localPath"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to <paramref name="localPath"/> is not permitted.</exception>
        public static void Add(this IBuilder builder, DownloadRetrievalMethod retrievalMethod, ICommandExecutor executor, ITaskHandler handler, string? localPath = null)
        {
            #region Sanity checks
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (retrievalMethod == null) throw new ArgumentNullException(nameof(retrievalMethod));
            if (executor == null) throw new ArgumentNullException(nameof(executor));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            void Process(Stream stream)
            {
                retrievalMethod.SetMissing(executor, localPath);

                long size = stream.Length;
                if (retrievalMethod is Archive archive)
                    size -= archive.StartOffset;
                if (retrievalMethod.Size != size)
                    executor.Execute(SetValueCommand.For(() => retrievalMethod.Size, newValue: size));

                builder.Add(retrievalMethod, stream, handler);
            }

            handler.RunTask((localPath == null)
                ? new DownloadFile(retrievalMethod.Href, Process)
                : new ReadFile(localPath, Process));
        }

        /// <summary>
        /// Copies files or directories from another implementation fetched by an external 0install process.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="metadata">The path of the source and destination file or directory.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        public static void CopyFrom(this IBuilder builder, CopyFromStep metadata, ITaskHandler handler)
        {
            handler.RunTask(new SimpleTask(string.Format(Resources.FetchingExternal, metadata.ID), () =>
            {
                var startInfo = ProcessUtils.FromCommandLine(
                    Environment.GetEnvironmentVariable("ZEROINSTALL_EXTERNAL_FETCHER")
                 ?? (Environment.GetEnvironmentVariable("ZEROINSTALL") ?? "0install") + " fetch");
                startInfo.RedirectStandardInput = true;
                var process = startInfo.Start();

                process.StandardInput.WriteLine(new Feed {Elements = {metadata.Implementation}}.ToXmlString().Replace("\n", ""));
                process.WaitForExit();
                if (process.ExitCode != 0) throw new IOException(string.Format(Resources.FetchingExternalFailed, metadata.ID));
            }));

            string path = ImplementationStores.Default().GetPath(metadata.Implementation);
            builder.CopyFrom(metadata, path, handler);
        }
    }
}