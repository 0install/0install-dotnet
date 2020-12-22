// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Tar;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Creates a TAR archive from a directory. Preserves executable bits, symlinks, hardlinks and timestamps.
    /// </summary>
    public class TarGenerator : ArchiveGenerator
    {
        #region Stream
        private readonly TarOutputStream _tarStream;

        /// <summary>
        /// Prepares to generate a TAR archive from a directory.
        /// </summary>
        /// <param name="sourcePath">The path of the directory to capture/store in the archive.</param>
        /// <param name="stream">The stream to write the generated archive to. Will be disposed when the generator is disposed.</param>
        internal TarGenerator(string sourcePath, Stream stream)
            : base(sourcePath)
        {
            #region Sanity checks
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            #endregion

            _tarStream = new(stream, Encoding.UTF8);
        }

        protected override void Dispose(bool disposing) => _tarStream.Dispose();
        #endregion

        private readonly List<FileInfo> _previousFiles = new();

        /// <inheritdoc/>
        protected override void HandleFile(FileInfo file, bool executable = false)
        {
            #region Sanity checks
            if (file == null) throw new ArgumentNullException(nameof(file));
            #endregion

            var entry = new TarEntry(new TarHeader
            {
                Name = file.RelativeTo(SourceDirectory),
                ModTime = file.LastWriteTimeUtc,
                Mode = (executable ? TarExtractor.DefaultMode | TarExtractor.ExecuteMode : TarExtractor.DefaultMode)
            });

            var hardlinkTarget = _previousFiles.FirstOrDefault(previousFile => FileUtils.AreHardlinked(previousFile.FullName, file.FullName));
            if (hardlinkTarget != null)
            {
                entry.TarHeader.TypeFlag = TarHeader.LF_LINK;
                entry.TarHeader.LinkName = hardlinkTarget.RelativeTo(SourceDirectory);
                _tarStream.PutNextEntry(entry);
            }
            else
            {
                _previousFiles.Add(file);

                entry.Size = file.Length;
                _tarStream.PutNextEntry(entry);
                using var stream = file.OpenRead();
                stream.CopyToEx(_tarStream);
            }
            _tarStream.CloseEntry();
        }

        /// <inheritdoc/>
        protected override void HandleSymlink(FileSystemInfo symlink, string target)
        {
            #region Sanity checks
            if (symlink == null) throw new ArgumentNullException(nameof(symlink));
            if (target == null) throw new ArgumentNullException(nameof(target));
            #endregion

            var data = target.ToStream();
            _tarStream.PutNextEntry(new TarEntry(new TarHeader
            {
                Name = symlink.RelativeTo(SourceDirectory),
                TypeFlag = TarHeader.LF_SYMLINK,
                Size = data.Length
            }));
            data.WriteTo(_tarStream);
            _tarStream.CloseEntry();
        }

        /// <inheritdoc/>
        protected override void HandleDirectory(DirectoryInfo directory)
        {
            #region Sanity checks
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            #endregion

            _tarStream.PutNextEntry(new TarEntry(new TarHeader
            {
                Name = directory.RelativeTo(SourceDirectory),
                TypeFlag = TarHeader.LF_DIR,
                Mode = TarExtractor.DefaultMode | TarExtractor.ExecuteMode
            }));
            _tarStream.CloseEntry();
        }
    }
}
