// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Tasks;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a 7z archive.
    /// </summary>
    public class SevenZipExtractor : ArchiveExtractor
    {
        private readonly Stream _stream;

        /// <summary>
        /// Prepares to extract a 7z archive contained in a stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal SevenZipExtractor(Stream stream, string targetPath)
            : base(targetPath)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _stream.Dispose();
        }

        private bool _unitsByte;

        /// <inheritdoc/>
        protected override bool UnitsByte => _unitsByte;

        /// <inheritdoc/>
        protected override void ExtractArchive()
        {
            using var archive = SevenZipArchive.Open(_stream);
            State = TaskState.Data;
            _unitsByte = true;
            UnitsTotal = archive.TotalUncompressSize;

            foreach (var entry in archive.Entries)
            {
                string? relativePath = GetRelativePath(entry.Key.Replace('\\', '/'));
                if (relativePath == null) continue;

                if (entry.IsDirectory) DirectoryBuilder.CreateDirectory(relativePath, entry.LastModifiedTime?.ToUniversalTime());
                else
                {
                    CancellationToken.ThrowIfCancellationRequested();

                    string absolutePath = DirectoryBuilder.NewFilePath(relativePath, entry.LastModifiedTime?.ToUniversalTime());
                    using (var fileStream = File.Create(absolutePath))
                        entry.WriteTo(fileStream);

                    UnitsProcessed += entry.Size;
                }
            }
        }
    }
}
