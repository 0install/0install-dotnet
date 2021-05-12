// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using NanoByte.Common;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations.Build;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Creates an archive from a directory. Preserves executable bits, symlinks and timestamps.
    /// </summary>
    public abstract class ArchiveGenerator : DirectoryTaskBase, IDisposable
    {
        /// <summary>
        /// All supported MIME types for creating archives. This is a subset of <see cref="Archive.KnownMimeTypes"/>
        /// </summary>
        public static readonly string[] SupportedMimeTypes = {Archive.MimeTypeZip, Archive.MimeTypeTar, Archive.MimeTypeTarGzip, Archive.MimeTypeTarBzip};

        /// <inheritdoc/>
        public override string Name => string.Format(Resources.CreatingArchive, OutputArchive);

        /// <summary>
        /// The path of the file to create.
        /// </summary>
        public string? OutputArchive { get; private set; }

        /// <summary>
        /// Prepares to generate an archive from a directory.
        /// </summary>
        /// <param name="sourcePath">The path of the directory to capture/store in the archive.</param>
        protected ArchiveGenerator(string sourcePath)
            : base(sourcePath)
        {}

        #region Factory methods
        /// <summary>
        /// Creates a new <see cref="ArchiveGenerator"/> for creating an archive from a directory and writing it to a stream.
        /// </summary>
        /// <param name="sourceDirectory">The path of the directory to capture/store in the archive.</param>
        /// <param name="stream">The stream to write the generated archive to. Will be disposed when the generator is disposed.</param>
        /// <param name="mimeType">The MIME type of archive format to create.</param>
        /// <exception cref="NotSupportedException">The <paramref name="mimeType"/> doesn't belong to a known and supported archive type.</exception>
        public static ArchiveGenerator Create(string sourceDirectory, Stream stream, string mimeType)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(sourceDirectory)) throw new ArgumentNullException(nameof(sourceDirectory));
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
            #endregion

            return mimeType switch
            {
                Archive.MimeTypeZip => new ZipGenerator(sourceDirectory, stream),
                Archive.MimeTypeTar => new TarGenerator(sourceDirectory, stream),
                Archive.MimeTypeTarGzip => new TarGzGenerator(sourceDirectory, stream),
                Archive.MimeTypeTarBzip => new TarBz2Generator(sourceDirectory, stream),
                _ => throw new NotSupportedException(string.Format(Resources.UnsupportedArchiveMimeType, mimeType))
            };
        }

        /// <summary>
        /// Creates a new <see cref="ArchiveGenerator"/> for creating an archive from a directory and writing it to a file.
        /// </summary>
        /// <param name="sourceDirectory">The path of the directory to capture/store in the archive.</param>
        /// <param name="path">The path of the archive file to create.</param>
        /// <param name="mimeType">The MIME type of archive format to create.</param>
        /// <exception cref="NotSupportedException">The <paramref name="mimeType"/> doesn't belong to a known and supported archive type.</exception>
        /// <exception cref="IOException">Failed to create the archive file.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the archive file was denied.</exception>
        public static ArchiveGenerator Create(string sourceDirectory, string path, string mimeType)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(sourceDirectory)) throw new ArgumentNullException(nameof(sourceDirectory));
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
            #endregion

            var generator = Create(sourceDirectory, File.Create(path), mimeType);
            generator.OutputArchive = path;
            return generator;
        }
        #endregion

        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Do not let exceptions in cleanup logic hide original exception.")]
        protected override void HandleEntries(IEnumerable<FileSystemInfo> entries)
        {
            try
            {
                base.HandleEntries(entries);
            }
            catch (OperationCanceledException)
            {
                if (OutputArchive != null && File.Exists(OutputArchive))
                {
                    Log.Info("Deleting incomplete archive " + OutputArchive);
                    try
                    {
                        Dispose();
                        File.Delete(OutputArchive);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(ex);
                    }
                }

                throw;
            }
        }

        #region Dispose
        /// <summary>
        /// Disposes the underlying <see cref="Stream"/>.
        /// </summary>
        public abstract void Dispose();
        #endregion
    }
}
