// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Tasks;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a 7z archive.
    /// </summary>
    public class SevenZipExtractor : ArchiveExtractor
    {
        private readonly SevenZipArchive _archive;

        /// <summary>
        /// Prepares to extract a 7z archive contained in a stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal SevenZipExtractor(Stream stream, string targetPath)
            : base(targetPath)
        {
            _archive = SevenZipArchive.Open(stream, new ReaderOptions {LeaveStreamOpen = false});
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _archive.Dispose();
        }

        /// <inheritdoc/>
        protected override void ExtractArchive()
        {
            State = TaskState.Data;

            try
            {
                UnitsTotal = _archive.TotalUncompressSize;

                var reader = _archive.ExtractAllEntries();
                while (reader.MoveToNextEntry())
                {
                    var entry = reader.Entry;

                    string? relativePath = GetRelativePath(entry.Key.Replace('\\', '/'));
                    if (relativePath == null) continue;

                    if (entry.IsDirectory) DirectoryBuilder.CreateDirectory(relativePath, entry.LastModifiedTime?.ToUniversalTime());
                    else
                    {
                        CancellationToken.ThrowIfCancellationRequested();

                        string absolutePath = DirectoryBuilder.NewFilePath(relativePath, entry.LastModifiedTime?.ToUniversalTime());
                        using (var fileStream = File.Create(absolutePath))
                            reader.WriteEntryTo(fileStream);

                        UnitsProcessed += entry.Size;
                    }
                }
            }
            #region Error handling
            catch (InvalidOperationException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid + "\n" + ex.Message, ex);
            }
            catch (ExtractionException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid + "\n" + ex.Message, ex);
            }
            #endregion
        }
    }
}
