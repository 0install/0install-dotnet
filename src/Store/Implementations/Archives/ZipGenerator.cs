// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Creates a ZIP archive from a directory. Preserves executable bits, symlinks and timestamps.
    /// </summary>
    public class ZipGenerator : ArchiveGenerator
    {
        #region Stream
        private readonly ZipOutputStream _zipStream;

        /// <summary>
        /// Prepares to generate a ZIP archive from a directory.
        /// </summary>
        /// <param name="sourcePath">The path of the directory to capture/store in the archive.</param>
        /// <param name="stream">The stream to write the generated archive to. Will be disposed when the generator is disposed.</param>
        internal ZipGenerator(string sourcePath, Stream stream)
            : base(sourcePath)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            #endregion

            _zipStream = new(stream);
        }

        public override void Dispose()
        {
            _zipStream.Dispose();
        }
        #endregion

        /// <inheritdoc/>
        protected override void HandleFile(FileInfo file, bool executable = false)
        {
            #region Sanity checks
            if (file == null) throw new ArgumentNullException(nameof(file));
            #endregion

            var entry = new ZipEntry(file.RelativeTo(SourceDirectory))
            {
                Size = file.Length,
                DateTime = file.LastWriteTimeUtc,
                HostSystem = (int)HostSystemID.Unix,
                ExtraData = GetUnixTimestamp(file.LastWriteTimeUtc)
            };
            if (executable)
                entry.ExternalFileAttributes = ZipExtractor.DefaultAttributes | ZipExtractor.ExecuteAttributes;
            _zipStream.PutNextEntry(entry);
            using var stream = file.OpenRead();
            stream.CopyToEx(_zipStream);
        }

        /// <summary>
        /// Encodes a <paramref name="timestamp"/> as a <see cref="ZipEntry.ExtraData"/> in a format that ensures it will be read for <see cref="ZipEntry.DateTime"/>, preserving second-accuracy.
        /// </summary>
        private static byte[] GetUnixTimestamp(DateTime timestamp)
        {
            var extraData = new ZipExtraData();
            extraData.AddEntry(new ExtendedUnixData
            {
                AccessTime = timestamp,
                CreateTime = timestamp,
                ModificationTime = timestamp,
                Include = ExtendedUnixData.Flags.AccessTime | ExtendedUnixData.Flags.CreateTime | ExtendedUnixData.Flags.ModificationTime
            });
            return extraData.GetEntryData();
        }

        /// <inheritdoc/>
        protected override void HandleSymlink(FileSystemInfo symlink, string target)
        {
            #region Sanity checks
            if (symlink == null) throw new ArgumentNullException(nameof(symlink));
            if (target == null) throw new ArgumentNullException(nameof(target));
            #endregion

            var data = target.ToStream();
            _zipStream.PutNextEntry(new ZipEntry(symlink.RelativeTo(SourceDirectory))
            {
                Size = data.Length,
                HostSystem = (int)HostSystemID.Unix,
                ExternalFileAttributes = ZipExtractor.DefaultAttributes | ZipExtractor.SymlinkAttributes
            });
            data.WriteTo(_zipStream);
        }

        /// <inheritdoc/>
        protected override void HandleDirectory(DirectoryInfo directory)
        {
            #region Sanity checks
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            #endregion

            _zipStream.PutNextEntry(new ZipEntry(directory.RelativeTo(SourceDirectory) + '/'));
        }
    }
}
