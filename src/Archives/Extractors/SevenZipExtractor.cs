// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts 7-zip archives (.7z).
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
[PrimaryConstructor]
public partial class SevenZipExtractor : ArchiveExtractor
{
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
                        using var elementStream = reader.OpenEntryStream().WithLength(entry.Size);
                        builder.AddFile(relativePath, elementStream, entry.LastModifiedTime ?? new UnixTime());
                    }
                }
            }
            #region Error handling
            catch (Exception ex) when (ex is InvalidOperationException or ExtractionException or IndexOutOfRangeException)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid + "\n" + ex.Message, ex);
            }
            #endregion
        });
    }
}
