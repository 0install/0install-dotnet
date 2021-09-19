// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Tar;
using NanoByte.Common;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using ZeroInstall.Archives.Extractors;

namespace ZeroInstall.Archives.Builders
{
    /// <summary>
    /// Builds a TAR archive (.tar).
    /// </summary>
    public class TarBuilder : IArchiveBuilder
    {
        private readonly TarOutputStream _tarStream;

        /// <summary>
        /// Creates a TAR archive builder.
        /// </summary>
        /// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
        public TarBuilder(Stream stream)
        {
            _tarStream = new(stream, Encoding.UTF8);
        }

        public void Dispose()
            => _tarStream.Dispose();

        /// <inheritdoc/>
        public void AddDirectory(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            _tarStream.PutNextEntry(new TarEntry(new TarHeader
            {
                Name = path.ToUnixPath(),
                TypeFlag = TarHeader.LF_DIR,
                Mode = TarExtractor.DefaultMode | TarExtractor.ExecuteMode
            }));
            _tarStream.CloseEntry();
        }

        private readonly Dictionary<string, UnixTime> _modifiedTimes = new();

        /// <inheritdoc/>
        public void AddFile(string path, Stream stream, UnixTime modifiedTime, bool executable = false)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            #endregion

            _tarStream.PutNextEntry(new TarEntry(new TarHeader
            {
                Name = path.ToUnixPath(),
                ModTime = modifiedTime,
                Mode = executable ? TarExtractor.DefaultMode | TarExtractor.ExecuteMode : TarExtractor.DefaultMode,
                Size = stream.Length
            }));
            stream.CopyToEx(_tarStream);
            _tarStream.CloseEntry();

            _modifiedTimes.Add(path, modifiedTime);
        }

        /// <inheritdoc/>
        public void AddSymlink(string path, string target)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (target == null) throw new ArgumentNullException(nameof(target));
            #endregion

            var data = target.ToUnixPath().ToStream();
            _tarStream.PutNextEntry(new TarEntry(new TarHeader
            {
                Name = path.ToUnixPath(),
                TypeFlag = TarHeader.LF_SYMLINK,
                Size = data.Length
            }));
            data.WriteTo(_tarStream);
            _tarStream.CloseEntry();
        }

        /// <inheritdoc/>
        public void AddHardlink(string path, string target, bool executable = false)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(target)) throw new ArgumentNullException(nameof(target));
            #endregion

            _tarStream.PutNextEntry(new TarEntry(new TarHeader
            {
                Name = path.ToUnixPath(),
                ModTime = _modifiedTimes[target],
                Mode = (executable ? TarExtractor.DefaultMode | TarExtractor.ExecuteMode : TarExtractor.DefaultMode),
            })
            {
                TarHeader =
                {
                    TypeFlag = TarHeader.LF_LINK,
                    LinkName = target.ToUnixPath()
                }
            });
            _tarStream.CloseEntry();
        }
    }
}