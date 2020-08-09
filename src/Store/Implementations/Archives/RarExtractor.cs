// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common.Tasks;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Readers.Rar;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a RAR archive.
    /// </summary>
    public class RarExtractor : ArchiveExtractor
    {
        private readonly RarReader _reader;

        /// <summary>
        /// Prepares to extract a RAR archive contained in a stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal RarExtractor(Stream stream, string targetPath)
            : base(targetPath)
        {
            _reader = RarReader.Open(stream, new ReaderOptions {LeaveStreamOpen = false});
            UnitsTotal = stream.Length;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _reader.Dispose();
        }

        /// <inheritdoc/>
        protected override void ExtractArchive()
        {
            State = TaskState.Data;

            try
            {
                while (_reader.MoveToNextEntry())
                {
                    var entry = _reader.Entry;

                    string? relativePath = GetRelativePath(entry.Key.Replace('\\', '/'));
                    if (relativePath == null) continue;

                    if (entry.IsDirectory) DirectoryBuilder.CreateDirectory(relativePath, entry.LastModifiedTime?.ToUniversalTime());
                    else
                    {
                        CancellationToken.ThrowIfCancellationRequested();

                        string absolutePath = DirectoryBuilder.NewFilePath(relativePath, entry.LastModifiedTime?.ToUniversalTime());
                        using (var fileStream = File.Create(absolutePath))
                            _reader.WriteEntryTo(fileStream);

                        UnitsProcessed += entry.CompressedSize;
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
