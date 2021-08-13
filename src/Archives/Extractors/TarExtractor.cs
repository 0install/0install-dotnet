// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Tar;
using NanoByte.Common.Tasks;
using ZeroInstall.Archives.Properties;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors
{
    /// <summary>
    /// Extracts TAR archives (.tar).
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public class TarExtractor : ArchiveExtractor
    {
        /// <summary>
        /// Creates a TAR extractor.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
        public TarExtractor(ITaskHandler handler)
            : base(handler)
        {}

        /// <inheritdoc/>
        public override void Extract(IBuilder builder, Stream stream, string? subDir = null)
        {
            var symlinks = new List<(string path, string target)>();
            var hardlinks = new List<(string path, string existingPath, bool executable)>();

            try
            {
                using var tarStream = new TarInputStream(stream, Encoding.UTF8) {IsStreamOwner = false};

                TarEntry entry;
                while ((entry = tarStream.GetNextEntry()) != null)
                {
                    Handler.CancellationToken.ThrowIfCancellationRequested();

                    string? relativePath = NormalizePath(entry.Name, subDir);
                    if (string.IsNullOrEmpty(relativePath)) continue;

                    switch (entry.TarHeader.TypeFlag)
                    {
                        case TarHeader.LF_SYMLINK:
                            symlinks.Add((relativePath, entry.TarHeader.LinkName));
                            break;
                        case TarHeader.LF_LINK:
                            string? targetPath = NormalizePath(entry.TarHeader.LinkName, subDir);
                            if (string.IsNullOrEmpty(targetPath)) throw new IOException($"The hardlink '{relativePath}' in the archive points to a non-existent file '{entry.TarHeader.LinkName}'.");
                            hardlinks.Add((relativePath, targetPath, IsExecutable(entry)));
                            break;
                        case TarHeader.LF_DIR:
                            builder.AddDirectory(relativePath);
                            break;
                        case TarHeader.LF_NORMAL or TarHeader.LF_OLDNORM:
                            builder.AddFile(relativePath, tarStream, entry.TarHeader.ModTime, IsExecutable(entry));
                            break;
                        default:
                            throw new NotSupportedException($"Archive entry '{entry.Name}' has unsupported type: {entry.TarHeader.TypeFlag}");
                    }
                }

                foreach ((string path, string target) in symlinks)
                    builder.AddSymlink(path, target);

                foreach ((string path, string existingPath, bool executable) in hardlinks)
                    builder.AddHardlink(path, existingPath, executable);
            }
            #region Error handling
            catch (Exception ex) when (ex is SharpZipBaseException or InvalidDataException or ArgumentOutOfRangeException or {Message: "Data Error"})
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(Resources.ArchiveInvalid, ex);
            }
            #endregion
        }

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
    }
}
