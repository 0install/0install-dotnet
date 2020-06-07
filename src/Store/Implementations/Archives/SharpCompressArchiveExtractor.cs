// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Tasks;
using SharpCompress.Archives;
using SharpCompress.Common;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    public abstract class SharpCompressArchiveExtractor : ArchiveExtractor
    {
        private readonly IArchive _archive;

        /// <summary>
        /// Prepares to extract a 7z archive contained in a stream.
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal SharpCompressArchiveExtractor(IArchive archive, string targetPath)
            : base(targetPath)
        {
            _archive = archive ?? throw new ArgumentNullException(nameof(archive));
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

                foreach (var entry in _archive.Entries)
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
            #region Error handling
            catch (ExtractionException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }
    }
}
