// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using SevenZip;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a 7z archive.
    /// </summary>
    public class SevenZipExtractor : ArchiveExtractor
    {
        #region Stream
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
            if (!WindowsUtils.IsWindows) throw new NotSupportedException(Resources.ExtractionOnlyOnWindows);

            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _stream.Dispose();
        }
        #endregion

        private bool _unitsByte;

        /// <inheritdoc/>
        protected override bool UnitsByte => _unitsByte;

        /// <inheritdoc/>
        protected override void ExtractArchive()
        {
            try
            {
                // NOTE: Must do initialization here since the constructor may be called on a different thread and SevenZipSharp is thread-affine
                SevenZipBase.SetLibraryPath(Locations.GetInstalledFilePath(OSUtils.Is64BitProcess ? @"x64\7z.dll" : @"x86\7z.dll"));

                using var extractor = new SevenZip.SevenZipExtractor(_stream);
                State = TaskState.Data;
                if (extractor.IsSolid || string.IsNullOrEmpty(Extract)) ExtractComplete(extractor);
                else ExtractIndividual(extractor);
            }
            #region Error handling
            catch (ObjectDisposedException ex)
            {
                // Async cancellation may cause underlying file stream to be closed
                Log.Warn(ex);
                throw new OperationCanceledException();
            }
            catch (SevenZipException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            catch (KeyNotFoundException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            catch (ArgumentException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            catch (NullReferenceException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }

        /// <summary>
        /// Extracts all files from the archive in one go.
        /// </summary>
        private void ExtractComplete(SevenZip.SevenZipExtractor extractor)
        {
            _unitsByte = false;
            UnitsTotal = 100;
            extractor.Extracting += (sender, e) => UnitsProcessed = e.PercentDone;

            CancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(Extract)) extractor.ExtractArchive(DirectoryBuilder.EffectiveTargetPath);
            else
            {
                // Use an intermediate temp directory (on the same filesystem)
                string tempDir = Path.Combine(TargetPath, Path.GetRandomFileName());
                extractor.ExtractArchive(tempDir);

                // Get only a specific subdir even though we extracted everything
                string subDir = FileUtils.UnifySlashes(Extract);
                string tempSubDir = Path.Combine(tempDir, subDir);
                if (!FileUtils.IsBreakoutPath(subDir) && Directory.Exists(tempSubDir))
                    new MoveDirectory(tempSubDir, DirectoryBuilder.EffectiveTargetPath, overwrite: true).Run(CancellationToken);
                Directory.Delete(tempDir, recursive: true);
            }
            CancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Extracts files from the archive one-by-one.
        /// </summary>
        private void ExtractIndividual(SevenZip.SevenZipExtractor extractor)
        {
            _unitsByte = true;
            UnitsTotal = extractor.UnpackedSize;

            foreach (var entry in extractor.ArchiveFileData)
            {
                string relativePath = GetRelativePath(entry.FileName.Replace('\\', '/'));
                if (relativePath == null) continue;

                if (entry.IsDirectory) DirectoryBuilder.CreateDirectory(relativePath, entry.LastWriteTime);
                else
                {
                    CancellationToken.ThrowIfCancellationRequested();

                    string absolutePath = DirectoryBuilder.NewFilePath(relativePath, entry.LastWriteTime);
                    using (var fileStream = File.Create(absolutePath))
                        extractor.ExtractFile(entry.Index, fileStream);

                    UnitsProcessed += (long)entry.Size;
                }
            }
        }
    }
}
#endif
