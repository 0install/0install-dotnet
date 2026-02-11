// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !MINIMAL
using NanoByte.Common.Streams;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts 7-zip archives (.7z).
/// </summary>
/// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class SevenZipExtractor(ITaskHandler handler) : ArchiveExtractor(handler)
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
    {
        EnsureSeekable(stream, seekableStream =>
        {
            try
            {
                using var reader = SevenZipArchive.OpenArchive(seekableStream, new() {LeaveStreamOpen = true}).ExtractAllEntries();
                while (reader.MoveToNextEntry())
                {
                    Handler.CancellationToken.ThrowIfCancellationRequested();

                    var entry = reader.Entry;

                    if (NormalizePath(entry.Key, subDir) is {} relativePath)
                    {
                        if (entry.IsDirectory) builder.AddDirectory(relativePath);
                        else
                        {
                            using var elementStream = reader.OpenEntryStream().WithLength(entry.Size);
                            builder.AddFile(relativePath, elementStream, entry.LastModifiedTime ?? new UnixTime());
                        }
                    }
                }
            }
            #region Error handling
            catch (Exception ex) when (ex is InvalidOperationException or ExtractionException or IndexOutOfRangeException)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        });
    }
}
#endif
