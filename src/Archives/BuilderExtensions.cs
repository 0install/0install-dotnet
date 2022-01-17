// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Tasks;
using ZeroInstall.Archives.Extractors;
using ZeroInstall.Model;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives;

/// <summary>
/// Helpers for adding <see cref="Archive"/>s to <see cref="IBuilder"/>s.
/// </summary>
public static class BuilderExtensions
{
    /// <summary>
    /// Adds a downloaded file to the implementation.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="retrievalMethod">The metadata of the file.</param>
    /// <param name="stream">The contents of the file.</param>
    /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
    /// <param name="tag">A <see cref="ITask.Tag"/> used to group progress bars. Usually <see cref="ManifestDigest.Best"/>.</param>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    public static void Add(this IBuilder builder, DownloadRetrievalMethod retrievalMethod, Stream stream, ITaskHandler handler, object? tag = null)
    {
        switch (retrievalMethod)
        {
            case SingleFile singleFile:
                builder.AddFile(singleFile, stream);
                break;
            case Archive archive:
                builder.AddArchive(archive, stream, handler, tag);
                break;
            default:
                throw new NotSupportedException($"Unknown download retrieval method: ${retrievalMethod}");
        }
    }

    /// <summary>
    /// Adds an archive to the implementation.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="archive">The metadata of the archive.</param>
    /// <param name="stream">The archive data to be extracted.</param>
    /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
    /// <param name="tag">A <see cref="ITask.Tag"/> used to group progress bars. Usually <see cref="ManifestDigest.Best"/>.</param>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    /// <exception cref="IOException">A problem occurred while extracting the archive.</exception>
    public static void AddArchive(this IBuilder builder, Archive archive, Stream stream, ITaskHandler handler, object? tag = null)
    {
        var extractor = ArchiveExtractor.For(archive.MimeType ?? throw new ArgumentException($"{nameof(Archive.MimeType)} not set.", nameof(archive)), handler);
        extractor.Tag = tag;
        extractor.Extract(builder.BuildDirectory(archive.Destination), stream, archive.Extract);
    }
}
