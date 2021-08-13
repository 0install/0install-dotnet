// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using ZeroInstall.Archives.Properties;
using ZeroInstall.Model;

namespace ZeroInstall.Archives.Builders
{
    /// <summary>
    /// Builds implementation archive files.
    /// </summary>
    public static class ArchiveBuilder
    {
        /// <summary>
        /// All supported MIME types for creating archives. This is a subset of <see cref="Archive.KnownMimeTypes"/>
        /// </summary>
        public static readonly string[] SupportedMimeTypes = {Archive.MimeTypeZip, Archive.MimeTypeTar, Archive.MimeTypeTarGzip, Archive.MimeTypeTarBzip};

        /// <summary>
        /// Creates a new <see cref="ArchiveBuilder"/> for creating an archive from a directory and writing it to a stream.
        /// </summary>
        /// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
        /// <param name="mimeType">The MIME type of archive format to create.</param>
        /// <exception cref="NotSupportedException">The <paramref name="mimeType"/> doesn't belong to a known and supported archive type.</exception>
        public static IArchiveBuilder Create(Stream stream, string mimeType)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
            #endregion

            return mimeType switch
            {
                Archive.MimeTypeZip => new ZipBuilder(stream),
                Archive.MimeTypeTar => new TarBuilder(stream),
                Archive.MimeTypeTarGzip => new TarGzBuilder(stream),
                Archive.MimeTypeTarBzip => new TarBz2Builder(stream),
                _ => throw new NotSupportedException(string.Format(Resources.UnsupportedArchiveMimeType, mimeType))
            };
        }

        /// <summary>
        /// Creates a new <see cref="ArchiveBuilder"/> for creating an archive from a directory and writing it to a file.
        /// </summary>
        /// <param name="path">The path of the archive file to create.</param>
        /// <param name="mimeType">The MIME type of archive format to create.</param>
        /// <exception cref="NotSupportedException">The <paramref name="mimeType"/> doesn't belong to a known and supported archive type.</exception>
        /// <exception cref="IOException">Failed to create the archive file.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the archive file was denied.</exception>
        public static IArchiveBuilder Create(string path, string mimeType)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
            #endregion

            return Create(File.Create(path), mimeType);
        }
    }
}
