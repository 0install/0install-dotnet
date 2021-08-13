// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using ZeroInstall.Archives.Properties;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors
{
    /// <summary>
    /// Extracts 7-zip archives (.7z).
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public class SevenZipExtractor : ArchiveExtractor
    {
        /// <summary>
        /// Creates a 7-zip extractor.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        public SevenZipExtractor(ITaskHandler handler)
            : base(handler)
        {}

        /// <inheritdoc/>
        public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
        {
            EnsureSeekable(stream, seekableStream =>
            {
                try
                {
                    var reader = SevenZipArchive.Open(seekableStream).ExtractAllEntries();
                    while (reader.MoveToNextEntry())
                    {
                        Handler.CancellationToken.ThrowIfCancellationRequested();

                        var entry = reader.Entry;

                        string? relativePath = NormalizePath(entry.Key, subDir);
                        if (relativePath == null) continue;

                        if (entry.IsDirectory) builder.AddDirectory(relativePath);
                        else
                        {
                            using var elementStream = reader.OpenEntryStream();
                            builder.AddFile(relativePath, elementStream, entry.LastModifiedTime ?? new UnixTime());
                        }
                    }
                }
                #region Error handling
                catch (Exception ex) when (ex is InvalidOperationException or ExtractionException)
                {
                    // Wrap exception since only certain exception types are allowed
                    throw new IOException(Resources.ArchiveInvalid + "\n" + ex.Message, ex);
                }
                #endregion
            });
        }
    }
}
