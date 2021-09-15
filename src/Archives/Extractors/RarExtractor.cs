// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using SharpCompress.Common;
using SharpCompress.Readers.Rar;
using ZeroInstall.Archives.Properties;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors
{
    /// <summary>
    /// Extracts RAR archives (.rar).
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    [PrimaryConstructor]
    public partial class RarExtractor : ArchiveExtractor
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

                    string? relativePath = NormalizePath(entry.Key, subDir);
                    if (relativePath == null) continue;

                    if (entry.IsDirectory) builder.AddDirectory(relativePath);
                    else
                    {
                        using var elementStream = reader.OpenEntryStream();
                        builder.AddFile(relativePath, elementStream, entry.LastModifiedTime ?? default);
                    }
                }
            }
            #region Error handling
            catch (ExtractionException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid + "\n" + ex.Message, ex);
            }
            #endregion
        }
    }
}
