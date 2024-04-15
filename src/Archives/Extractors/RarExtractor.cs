// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using SharpCompress.Common;
using SharpCompress.Readers.Rar;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts RAR archives (.rar).
/// </summary>
/// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
/// <remarks>This class is immutable and thread-safe.</remarks>
public class RarExtractor(ITaskHandler handler) : ArchiveExtractor(handler)
{
    /// <inheritdoc/>
    public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
    {
        try
        {
            using var reader = RarReader.Open(stream, new() {LeaveStreamOpen = true});
            while (reader.MoveToNextEntry())
            {
                Handler.CancellationToken.ThrowIfCancellationRequested();

                var entry = reader.Entry;

                if (NormalizePath(entry.Key, subDir) is {} relativePath)
                {
                    if (entry.IsDirectory) builder.AddDirectory(relativePath);
                    else
                    {
                        using var elementStream = reader.OpenEntryStream();
                        builder.AddFile(relativePath, elementStream, entry.LastModifiedTime ?? default);
                    }
                }
            }
        }
        #region Error handling
        catch (ExtractionException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new IOException(Resources.ArchiveInvalid, ex);
        }
        #endregion
    }
}
