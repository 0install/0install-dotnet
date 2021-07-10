// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts implementation archives.
    /// </summary>
    public abstract class ArchiveExtractor : IArchiveExtractor
    {
        /// <summary>
        /// Creates a new <see cref="ArchiveExtractor"/> for extracting from an archive stream.
        /// </summary>
        /// <param name="mimeType">The MIME type of archive format of the stream.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        public static IArchiveExtractor For(string mimeType, ITaskHandler handler)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

#if NETFRAMEWORK
            // Extracted to separate functions delay loading of Microsoft.Deployment* DLLs
            ArchiveExtractor NewCabExtractor() => new CabExtractor(handler);
            ArchiveExtractor NewMsiExtractor() => new MsiExtractor(handler);
#endif

            return mimeType switch
            {
                Archive.MimeTypeZip => new ZipExtractor(handler),
                Archive.MimeTypeTar => new TarExtractor(handler),
                Archive.MimeTypeTarGzip => new TarGzExtractor(handler),
                Archive.MimeTypeTarBzip => new TarBz2Extractor(handler),
                Archive.MimeTypeTarLzma => new TarLzmaExtractor(handler),
                Archive.MimeTypeTarLzip => new TarLzipExtractor(handler),
                Archive.MimeTypeTarXz => new TarXzExtractor(handler),
                Archive.MimeTypeTarZstandard => new TarZstandardExtractor(handler),
                Archive.MimeType7Z => new SevenZipExtractor(handler),
                Archive.MimeTypeRar => new RarExtractor(handler),
#if NETFRAMEWORK
                Archive.MimeTypeCab => NewCabExtractor(),
                Archive.MimeTypeMsi => NewMsiExtractor(),
#endif
                Archive.MimeTypeRubyGem => new RubyGemExtractor(handler),
                _ => throw new NotSupportedException(string.Format(Resources.UnsupportedArchiveMimeType, mimeType))
            };
        }

        /// <summary>
        /// Checks whether archives of a specific MIME type are supported.
        /// </summary>
        /// <param name="mimeType">The MIME type of archive format to check.</param>
        public static bool IsSupported(string mimeType)
        {
            try
            {
                For(mimeType, new SilentTaskHandler());
                return true;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        /// <summary>
        /// A callback object used when the the user needs to be informed about IO tasks.
        /// </summary>
        protected readonly ITaskHandler Handler;

        /// <summary>
        /// Creates an archive extractor.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        protected ArchiveExtractor(ITaskHandler handler)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <inheritdoc/>
        public abstract void Extract(IImplementationBuilder builder, Stream stream, string? subDir = null);

        /// <inheritdoc/>
        public object? Tag { get; set; }

        /// <summary>
        /// Ensures that a <see cref="Stream"/> is seekable, creating a temporary on-disk copy if necessary.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="callback">Called with the original <paramref name="stream"/> or a temporary seekable copy.</param>
        protected void EnsureCanSeek(Stream stream, [InstantHandle] Action<Stream> callback)
        {
            if (stream.CanSeek)
                callback(stream);
            else
            {
                using var tempFile = new TemporaryFile("0install-archive");
                stream.CopyToFile(tempFile);
                Handler.RunTask(new ReadFile(tempFile, callback) {Tag = Tag});
            }
        }

        /// <summary>
        /// Ensures that a <see cref="Stream"/> represents an on-disk file, creating a temporary on-disk copy if necessary.
        /// </summary>
        /// <param name="stream">The stream to read. May be <see cref="Stream.Close"/>d.</param>
        /// <param name="callback">Called with the file path.</param>
        protected static void EnsureFile(Stream stream, [InstantHandle] Action<string> callback)
        {
            if (stream is FileStream fileStream)
            {
                fileStream.Close();
                callback(fileStream.Name);
            }
            else
            {
                using var tempFile = new TemporaryFile("0install-archive");
                stream.CopyToFile(tempFile);
                callback(tempFile);
            }
        }

        /// <summary>
        /// Normalizes the path of an archive entry.
        /// </summary>
        /// <param name="path">The Unix-style path of the archive entry relative to the archive's root.</param>
        /// <param name="subDir">The Unix-style path of the subdirectory in the archive to extract; <c>null</c> to extract entire archive.</param>
        /// <returns>The relative path without the <paramref name="subDir"/>; <c>null</c> if the <paramref name="path"/> doesn't lie within the <paramref name="subDir"/>.</returns>
        protected static string? NormalizePath(string path, string? subDir)
        {
            path = path.ToNativePath().Trim(Path.DirectorySeparatorChar);
            if (path.StartsWith("." + Path.DirectorySeparatorChar, out string? rest))
                path = rest;
            if (path == ".") return null;

            if (string.IsNullOrEmpty(subDir)) return path;
            subDir = subDir.ToNativePath().Trim(Path.DirectorySeparatorChar);
            if (path.StartsWith(subDir + Path.DirectorySeparatorChar, out rest))
                return rest;
            return null;
        }
    }
}
