// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using NanoByte.Common.Values;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts ZIP archives (.zip).
    /// </summary>
    public class ZipExtractor : ArchiveExtractor
    {
        /// <summary>
        /// Creates a ZIP extractor.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        public ZipExtractor(ITaskHandler handler)
            : base(handler)
        {}

        /// <inheritdoc/>
        public override void Extract(IImplementationBuilder builder, Stream stream, string? subDir = null)
        {
            EnsureCanSeek(stream, seekableStream =>
            {
                try
                {
                    var centralDirectory = ReadCentralDirectory(seekableStream);
                    Extract(builder, new ZipInputStream(seekableStream), centralDirectory, subDir);
                }
                #region Error handling
                catch (Exception ex) when (ex is SharpZipBaseException or InvalidDataException or ArgumentOutOfRangeException)
                {
                    // Wrap exception since only certain exception types are allowed
                    throw new IOException(Resources.ArchiveInvalid, ex);
                }
                #endregion
            });
        }

        private static IEnumerable<ZipEntry> ReadCentralDirectory(Stream stream)
        {
            var zipFile = new ZipFile(stream);
            var centralDirectory = new ZipEntry[zipFile.Count];
            for (int i = 0; i < centralDirectory.Length; i++)
                centralDirectory[i] = zipFile[i];
            stream.Seek(0, SeekOrigin.Begin);
            return centralDirectory;
        }

        /// <summary>
        /// Reads a ZIP file sequentially and references its central directory in parallel.
        /// </summary>
        private void Extract(IImplementationBuilder builder, ZipInputStream zipStream, IEnumerable<ZipEntry> centralDirectory, string? subDir)
        {
            foreach (var centralEntry in centralDirectory)
            {
                Handler.CancellationToken.ThrowIfCancellationRequested();

                var localEntry = zipStream.GetNextEntry();
                if (localEntry == null) break;

                string? relativePath = NormalizePath(centralEntry.Name, subDir);
                if (string.IsNullOrEmpty(relativePath)) continue;

                if (centralEntry.IsDirectory) builder.AddDirectory(relativePath);
                else if (centralEntry.IsFile)
                {
                    if (IsSymlink(centralEntry))
                        builder.AddSymlink(relativePath, zipStream.ReadToString());
                    else
                        builder.AddFile(relativePath, zipStream, localEntry.DateTime, IsExecutable(centralEntry));
                }
            }
        }

        /// <summary>
        /// The default <see cref="ZipEntry.ExternalFileAttributes"/>.
        /// </summary>
        public const int DefaultAttributes = (6 << 22) + (4 << 19) + (4 << 16); // Octal: 644

        /// <summary>
        /// The <see cref="ZipEntry.ExternalFileAttributes"/> that indicate a ZIP entry is a symlink.
        /// </summary>
        public const int SymlinkAttributes = 4 << 27; // Octal: 40000

        /// <summary>
        /// Determines whether a <see cref="ZipEntry"/> was created on a Unix-system with the symlink flag set.
        /// </summary>
        private static bool IsSymlink(ZipEntry entry)
            => (entry.HostSystem == (int)HostSystemID.Unix)
            && entry.ExternalFileAttributes.HasFlag(SymlinkAttributes);

        /// <summary>
        /// The <see cref="ZipEntry.ExternalFileAttributes"/> that indicate a ZIP entry is an executable file.
        /// </summary>
        public const int ExecuteAttributes = (1 << 22) + (1 << 19) + (1 << 16); // Octal: 111

        /// <summary>
        /// Determines whether a <see cref="ZipEntry"/> was created on a Unix-system with the executable flag set.
        /// </summary>
        private static bool IsExecutable(ZipEntry entry)
            => (entry.HostSystem == (int)HostSystemID.Unix)
            && (entry.ExternalFileAttributes & ExecuteAttributes) > 0; // Check if anybody is allowed to execute
    }
}
