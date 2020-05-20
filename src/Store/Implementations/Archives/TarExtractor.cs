// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Tar;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Extracts a TAR archive.
    /// </summary>
    public class TarExtractor : ArchiveExtractor
    {
        #region Stream
        private readonly TarInputStream _tarStream;

        /// <summary>
        /// Prepares to extract a TAR archive contained in a stream.
        /// </summary>
        /// <param name="stream">The stream containing the archive data to be extracted. Will be disposed when the extractor is disposed.</param>
        /// <param name="targetPath">The path to the directory to extract into.</param>
        /// <exception cref="IOException">The archive is damaged.</exception>
        internal TarExtractor(Stream stream, string targetPath)
            : base(targetPath)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            #endregion

            try
            {
                UnitsTotal = stream.Length;
            }
            catch (NotSupportedException)
            {}

            try
            {
                _tarStream = new TarInputStream(stream);
            }
            catch (SharpZipBaseException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tarStream.Dispose();
        }
        #endregion

        /// <inheritdoc/>
        protected override void ExtractArchive()
        {
            try
            {
                TarEntry entry;
                while ((entry = _tarStream.GetNextEntry()) != null)
                {
                    string? relativePath = GetRelativePath(entry.Name);
                    if (string.IsNullOrEmpty(relativePath)) continue;

                    switch (entry.TarHeader.TypeFlag)
                    {
                        case TarHeader.LF_DIR:
                            DirectoryBuilder.CreateDirectory(relativePath, entry.TarHeader.ModTime);
                            break;
                        case TarHeader.LF_LINK:
                        {
                            string? targetPath = GetRelativePath(entry.TarHeader.LinkName);
                            if (string.IsNullOrEmpty(targetPath)) throw new IOException(string.Format(Resources.HardlinkTargetMissing, relativePath, entry.TarHeader.LinkName));
                            DirectoryBuilder.QueueHardlink(relativePath, targetPath, IsExecutable(entry));
                            break;
                        }
                        case TarHeader.LF_SYMLINK:
                            DirectoryBuilder.CreateSymlink(relativePath, entry.TarHeader.LinkName);
                            break;
                        default:
                            WriteFile(relativePath, entry.Size, entry.TarHeader.ModTime, _tarStream, IsExecutable(entry));
                            break;
                    }

                    UpdateProgress();
                }
            }
            #region Error handling
            catch (SharpZipBaseException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            catch (InvalidDataException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }

        /// <summary>
        /// Updates <see cref="TaskBase.UnitsProcessed"/> to reflect the number of bytes extracted so far.
        /// </summary>
        protected virtual void UpdateProgress() => UnitsProcessed = _tarStream.Position;

        /// <summary>
        /// The default <see cref="TarHeader.Mode"/>.
        /// </summary>
        public const int DefaultMode = (6 << 6) + (4 << 3) + 4; // Octal: 644

        /// <summary>
        /// The <see cref="TarHeader.Mode"/> that indicate a TAR entry is an executable.
        /// </summary>
        public const int ExecuteMode = (1 << 6) + (1 << 3) + 1; // Octal: 111

        /// <summary>
        /// Determines whether a <see cref="TarEntry"/> was created with the executable flag set.
        /// </summary>
        private static bool IsExecutable(TarEntry entry) =>
            (entry.TarHeader.Mode & ExecuteMode) > 0; // Check if anybody is allowed to execute

        /// <summary>
        /// Helper method for <see cref="ArchiveExtractor.WriteFile"/>.
        /// </summary>
        /// <param name="stream">The <see cref="TarInputStream"/> containing the entry data to write to a file.</param>
        /// <param name="fileStream">Stream access to the file to write.</param>
        protected override void StreamToFile(Stream stream, FileStream fileStream)
            => _tarStream.CopyEntryContents(fileStream);
    }
}
