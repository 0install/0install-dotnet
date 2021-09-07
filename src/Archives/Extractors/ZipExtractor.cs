// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using NanoByte.Common.Values;
using ZeroInstall.Archives.Properties;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors
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
        public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
        {
            stream = stream.WithSeekBuffer();

            try
            {
                ExtractFiles(new ZipInputStream(stream) {IsStreamOwner = false}, subDir, builder);
                ApplyCentral(new ZipFile(stream, leaveOpen: true), subDir, builder);
            }
            #region Error handling
            catch (Exception ex) when (ex is SharpZipBaseException or InvalidDataException or ArgumentOutOfRangeException)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }

        private void ExtractFiles(ZipInputStream zipStream, string? subDir, IBuilder builder)
        {
            ZipEntry entry;
            while ((entry = zipStream.GetNextEntry()) != null)
            {
                Handler.CancellationToken.ThrowIfCancellationRequested();

                string? relativePath = NormalizePath(entry.Name, subDir);
                if (string.IsNullOrEmpty(relativePath)) continue;

                if (entry.IsDirectory)
                    builder.AddDirectory(relativePath);
                else if (entry.IsFile)
                    builder.AddFile(relativePath, zipStream, GetTimestamp(entry));
            }
        }

        private static DateTime GetTimestamp(ZipEntry entry)
            => new ZipExtraData(entry.ExtraData)
              .GetData<OldUnixExtraData>()
             ?.ModificationTime
            ?? entry.DateTime;

        private static void ApplyCentral(ZipFile zipFile, string? subDir, IBuilder builder)
        {
            for (int i = 0; i < zipFile.Count; i++)
            {
                var entry = zipFile[i];

                string? relativePath = NormalizePath(entry.Name, subDir);
                if (string.IsNullOrEmpty(relativePath)) continue;

                if (entry.IsDirectory)
                    builder.AddDirectory(relativePath);
                else if (entry.IsFile)
                {
                    if (IsSymlink(entry))
                        builder.TurnIntoSymlink(relativePath);
                    else if (IsExecutable(entry))
                        builder.MarkAsExecutable(relativePath);
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
