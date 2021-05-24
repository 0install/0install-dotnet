// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Copies the content of a directory to a new location preserving the original file modification times, executable bits and symlinks.
    /// </summary>
    public class CloneDirectory : DirectoryTaskBase
    {
        /// <summary>Used to build the target directory with support for flag files.</summary>
        protected readonly DirectoryBuilder DirectoryBuilder;

        /// <summary>
        /// The path to the directory to clone to.
        /// </summary>
        public string TargetPath => DirectoryBuilder.TargetPath;

        /// <summary>
        /// Sub-path to be appended to <see cref="TargetPath"/> without affecting location of flag files; <c>null</c> for none.
        /// </summary>
        public string? TargetSuffix { get => DirectoryBuilder.TargetSuffix; init => DirectoryBuilder.TargetSuffix = value; }

        /// <summary>
        /// Use hardlinks instead of copying files when possible.
        /// Only use this if you are sure the source files will not be modified!
        /// </summary>
        public bool UseHardlinks { get; init; }

        /// <summary>
        /// Creates a new directory cloning task.
        /// </summary>
        /// <param name="sourcePath">The path of the original directory to read.</param>
        /// <param name="targetPath">The path of the new directory to create.</param>
        public CloneDirectory(string sourcePath, string targetPath)
            : base(sourcePath)
        {
            DirectoryBuilder = new(targetPath ?? throw new ArgumentNullException(targetPath));
        }

        /// <inheritdoc/>
        public override string Name => Resources.CopyFiles;

        /// <inheritdoc/>
        protected override void HandleEntries(IEnumerable<FileSystemInfo> entries)
        {
            DirectoryBuilder.EnsureDirectory();

            using (ImplementationStoreUtils.TryUnseal(SourceDirectory.FullName))
            {
                base.HandleEntries(entries);
                DirectoryBuilder.CompletePending();
            }

            // Tries to remove write-protection on the directory, if it is located in a Store to allow creating hardlinks pointing into it.
        }

        /// <inheritdoc/>
        protected override void HandleFile(FileInfo file, bool executable = false)
        {
            #region Sanity checks
            if (file == null) throw new ArgumentNullException(nameof(file));
            #endregion

            if (UseHardlinks)
            {
                // Timestamps for hardlinked files are linked by the filesystem itself on Unixoid systems
                var lastWriteTime = UnixUtils.IsUnix ? (DateTime?)null : file.LastWriteTimeUtc;

                CopyFileAsHardlink(file.FullName, NewFilePath(file, lastWriteTime, executable));
            }
            else
                CopyFile(file.FullName, NewFilePath(file, file.LastWriteTimeUtc, executable));

            void CopyFileAsHardlink(string existingPath, string newPath)
            {
                try
                {
                    FileUtils.CreateHardlink(newPath, existingPath);
                }
                catch (PlatformNotSupportedException)
                {
                    CopyFile(existingPath, newPath);
                }
                catch (UnauthorizedAccessException)
                {
                    CopyFile(existingPath, newPath);
                }
            }

            void CopyFile(string existingPath, string newPath)
            {
                using var readStream = File.OpenRead(existingPath);
                using var writeStream = File.Create(newPath);
                readStream.CopyToEx(writeStream);
            }
        }

        /// <summary>
        /// Prepares a new file path in the directory without creating the file itself yet.
        /// </summary>
        /// <param name="originalFile">The original file to base the new one on.</param>
        /// <param name="lastWriteTime">The last write time to set for the file later. This value is optional.</param>
        /// <param name="executable"><c>true</c> if the file's executable bit is to be set later; <c>false</c> otherwise.</param>
        /// <returns>An absolute file path.</returns>
        protected virtual string NewFilePath(FileInfo originalFile, DateTime? lastWriteTime, bool executable)
        {
            if (originalFile == null) throw new ArgumentNullException(nameof(originalFile));
            return DirectoryBuilder.NewFilePath(originalFile.RelativeTo(SourceDirectory), lastWriteTime, executable);
        }

        /// <inheritdoc/>
        protected override void HandleSymlink(FileSystemInfo symlink, string target)
        {
            if (symlink == null) throw new ArgumentNullException(nameof(symlink));
            if (target == null) throw new ArgumentNullException(nameof(target));
            DirectoryBuilder.CreateSymlink(symlink.RelativeTo(SourceDirectory), target);
        }

        /// <inheritdoc/>
        protected override void HandleDirectory(DirectoryInfo directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            DirectoryBuilder.CreateDirectory(directory.RelativeTo(SourceDirectory), directory.LastWriteTimeUtc);
        }
    }
}
