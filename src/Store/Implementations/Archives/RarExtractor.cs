// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common.Tasks;
using SharpCompress.Common;
using SharpCompress.Readers.Rar;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts RAR archives (.rar).
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public class RarExtractor : ArchiveExtractor
    {
        /// <summary>
        /// Creates a RAR extractor.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        public RarExtractor(ITaskHandler handler)
            : base(handler)
        {}

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
