// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.IO;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations.Build;
using ZeroInstall.Store.Properties;

#if NETFRAMEWORK
using NanoByte.Common.Native;
#endif

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts an archive.
    /// </summary>
    public abstract class ArchiveExtractor : TaskBase, IDisposable
    {
        /// <inheritdoc/>
        public override string Name => Resources.ExtractingArchive;

        /// <inheritdoc/>
        protected override bool UnitsByte => true;

        /// <summary>
        /// The name of the subdirectory in the archive to extract (with Unix-style slashes); <c>null</c> to extract entire archive.
        /// </summary>
        [Description("The name of the subdirectory in the archive to extract (with Unix-style slashes); null to extract entire archive.")]
        public string? Extract { get; set; }

        /// <summary>Used to build the target directory with support for flag files.</summary>
        protected readonly DirectoryBuilder DirectoryBuilder;

        /// <summary>
        /// The path to the directory to extract into.
        /// </summary>
        [Description("The path to the directory to extract into.")]
        public string TargetPath => DirectoryBuilder.TargetPath;

        /// <summary>
        /// Sub-path to be appended to <see cref="TargetPath"/> without affecting location of flag files; <c>null</c> for none.
        /// </summary>
        [Description("Sub-path to be appended to TargetDir without affecting location of flag files.")]
        public string? TargetSuffix { get => DirectoryBuilder.TargetSuffix; set => DirectoryBuilder.TargetSuffix = value; }

        /// <summary>
        /// Prepares to extract an archive contained in a stream.
        /// </summary>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        protected ArchiveExtractor(string targetPath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(targetPath)) throw new ArgumentNullException(nameof(targetPath));
            #endregion

            DirectoryBuilder = new DirectoryBuilder(targetPath);
        }

        #region Factory methods
        /// <summary>
        /// Verifies that a archives of a specific MIME type are supported.
        /// </summary>
        /// <param name="mimeType">The MIME type of archive format of the stream.</param>
        /// <returns>The newly created <see cref="ArchiveExtractor"/>.</returns>
        /// <exception cref="NotSupportedException">The <paramref name="mimeType"/> doesn't belong to a known and supported archive type.</exception>
        public static void VerifySupport(string mimeType)
        {
            switch (mimeType)
            {
                case Archive.MimeTypeZip:
                case Archive.MimeTypeTar:
                case Archive.MimeTypeTarGzip:
                case Archive.MimeTypeTarBzip:
                case Archive.MimeTypeTarLzma:
                case Archive.MimeTypeTarLzip:
                case Archive.MimeTypeTarXz:
                case Archive.MimeTypeTarZstandard:
                case Archive.MimeTypeRubyGem:
                case Archive.MimeType7Z:
                case Archive.MimeTypeRar:
                    return;

#if NETFRAMEWORK
                case Archive.MimeTypeCab:
                case Archive.MimeTypeMsi:
                    if (!WindowsUtils.IsWindows) throw new NotSupportedException(Resources.ExtractionOnlyOnWindows);
                    return;
#endif

                default:
                    throw new NotSupportedException(string.Format(Resources.UnsupportedArchiveMimeType, mimeType));
            }
        }

        /// <summary>
        /// Creates a new <see cref="ArchiveExtractor"/> for extracting from an archive stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <param name="mimeType">The MIME type of archive format of the stream.</param>
        /// <exception cref="IOException">Failed to read the archive file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the archive file was denied.</exception>
        public static ArchiveExtractor Create(Stream stream, string targetPath, string mimeType)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (string.IsNullOrEmpty(targetPath)) throw new ArgumentNullException(nameof(targetPath));
            if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
            #endregion

#if NETFRAMEWORK
            // Extracted to separate function delay loading of Microsoft.Deployment* DLLs
            ArchiveExtractor NewCabExtractor() => new CabExtractor(stream, targetPath);
#endif

            return mimeType switch
            {
                Archive.MimeTypeZip => new ZipExtractor(stream, targetPath),
                Archive.MimeTypeTar => new TarExtractor(stream, targetPath),
                Archive.MimeTypeTarGzip => new TarGzExtractor(stream, targetPath),
                Archive.MimeTypeTarBzip => new TarBz2Extractor(stream, targetPath),
                Archive.MimeTypeTarLzma => new TarLzmaExtractor(stream, targetPath),
                Archive.MimeTypeTarLzip => new TarLzipExtractor(stream, targetPath),
                Archive.MimeTypeTarXz => new TarXzExtractor(stream, targetPath),
                Archive.MimeTypeTarZstandard => new TarZstandardExtractor(stream, targetPath),
                Archive.MimeType7Z => new SevenZipExtractor(stream, targetPath),
                Archive.MimeTypeRar => new RarExtractor(stream, targetPath),
#if NETFRAMEWORK
                Archive.MimeTypeCab => NewCabExtractor(),
                Archive.MimeTypeMsi => throw new NotSupportedException("MSIs can only be accessed as local files, not as streams!"),
#endif
                Archive.MimeTypeRubyGem => new RubyGemExtractor(stream, targetPath),
                _ => throw new NotSupportedException(string.Format(Resources.UnsupportedArchiveMimeType, mimeType))
            };
        }

        /// <summary>
        /// Creates a new <see cref="ArchiveExtractor"/> for extracting from an archive file.
        /// </summary>
        /// <param name="archivePath">The path of the archive file to be extracted.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <param name="mimeType">The MIME type of archive format of the stream.</param>
        /// <param name="startOffset"></param>
        /// <returns>The newly created <see cref="ArchiveExtractor"/>.</returns>
        /// <exception cref="IOException">The archive is damaged.</exception>
        /// <exception cref="NotSupportedException">The <paramref name="mimeType"/> doesn't belong to a known and supported archive type.</exception>
        public static ArchiveExtractor Create(string archivePath, string targetPath, string mimeType, long startOffset = 0)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(archivePath)) throw new ArgumentNullException(nameof(mimeType));
            if (string.IsNullOrEmpty(targetPath)) throw new ArgumentNullException(nameof(targetPath));
            if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
            #endregion

#if NETFRAMEWORK
            // Extracted to delay loading of Microsoft.Deployment* DLLs
            ArchiveExtractor NewMsiExtractor() => new MsiExtractor(archivePath, targetPath);

            // MSI Extractor does not support Stream-based access
            if (mimeType == Archive.MimeTypeMsi) return NewMsiExtractor();
#endif

            Stream stream = File.OpenRead(archivePath);
            if (startOffset != 0) stream = new OffsetStream(stream, startOffset);

            try
            {
                return Create(stream, targetPath, mimeType);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }
        #endregion

        /// <inheritdoc/>
        protected override void Execute()
        {
            State = TaskState.Data;
            DirectoryBuilder.EnsureDirectory();

            ExtractArchive();

            DirectoryBuilder.CompletePending();
        }

        /// <summary>
        /// Extracts the archive.
        /// </summary>
        /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
        /// <exception cref="IOException">A problem occurred while extracting the archive.</exception>
        protected abstract void ExtractArchive();

        /// <summary>
        /// Returns the path of an archive entry relative to <see cref="Extract"/>.
        /// </summary>
        /// <param name="entryName">The Unix-style path of the archive entry relative to the archive's root.</param>
        /// <returns>The relative path or <c>null</c> if the <paramref name="entryName"/> doesn't lie within the <see cref="Extract"/>.</returns>
        protected virtual string? GetRelativePath(string entryName)
        {
            entryName = FileUtils.UnifySlashes(entryName);

            // Remove leading slashes
            entryName = entryName.TrimStart(Path.DirectorySeparatorChar);
            if (entryName.StartsWith("." + Path.DirectorySeparatorChar)) entryName = entryName.Substring(2);

            if (!string.IsNullOrEmpty(Extract))
            {
                // Remove leading and trailing slashes
                string subDir = FileUtils.UnifySlashes(Extract)!.Trim(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

                // Only extract objects within the selected sub-directory
                if (!entryName.StartsWith(subDir)) return null;

                entryName = entryName.Substring(subDir.Length);
            }

            // Remove leading slashes left over after trimming away the SubDir
            return entryName.TrimStart(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Writes a file to the filesystem and sets its last write time.
        /// </summary>
        /// <param name="relativePath">A path relative to <see cref="Build.DirectoryBuilder.EffectiveTargetPath"/>.</param>
        /// <param name="fileSize">The length of the zip entries uncompressed data, needed because stream's Length property is always 0.</param>
        /// <param name="lastWriteTime">The last write time to set.</param>
        /// <param name="stream">The stream containing the file data to be written.</param>
        /// <param name="executable"><c>true</c> if the file's executable bit is set; <c>false</c> otherwise.</param>
        protected void WriteFile(string relativePath, long fileSize, DateTime lastWriteTime, Stream stream, bool executable = false)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(relativePath)) throw new ArgumentNullException(nameof(relativePath));
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            #endregion

            CancellationToken.ThrowIfCancellationRequested();

            string absolutePath = DirectoryBuilder.NewFilePath(relativePath, lastWriteTime, executable);
            try
            {
                using var fileStream = File.Create(absolutePath);
                if (fileSize != 0)
                    StreamToFile(stream, fileStream);
            }
            catch (DirectoryNotFoundException ex)
            {
                // Missing directories usually indicate problems with long paths
                throw new PathTooLongException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Helper method for <see cref="WriteFile"/>.
        /// </summary>
        /// <param name="stream">The stream to write to a file.</param>
        /// <param name="fileStream">Stream access to the file to write.</param>
        /// <remarks>Can be overwritten for archive formats that don't simply write a <see cref="Stream"/> to a file.</remarks>
        protected virtual void StreamToFile(Stream stream, FileStream fileStream)
            => stream.CopyToEx(fileStream, cancellationToken: CancellationToken);

        #region Dispose
        /// <summary>
        /// Disposes the underlying <see cref="Stream"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
        #endregion
    }
}
