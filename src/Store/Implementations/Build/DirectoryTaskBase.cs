// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Implementations.Manifests;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Common base class for tasks that walk an entire directory tree using template methods.
    /// </summary>
    public abstract class DirectoryTaskBase : TaskBase
    {
        /// <inheritdoc/>
        protected override bool UnitsByte => true;

        /// <summary>Indicates whether <see cref="SourceDirectory"/> is located on a filesystem with support for Unixoid features such as executable bits.</summary>
        private readonly bool _sourceIsUnixFS;

        /// <summary>
        /// The directory to walk.
        /// </summary>
        public DirectoryInfo SourceDirectory { get; }

        /// <summary>
        /// Creates a new directory walking task.
        /// </summary>
        /// <param name="sourcePath">The path of the directory to walk.</param>
        protected DirectoryTaskBase(string sourcePath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));
            #endregion

            SourceDirectory = new(Path.GetFullPath(sourcePath));
            _sourceIsUnixFS = FlagUtils.IsUnixFS(sourcePath);
        }

        /// <inheritdoc/>
        protected override void Execute()
        {
            State = TaskState.Header;
            var entries = GetSortedDirectoryEntries(SourceDirectory.FullName);
            UnitsTotal = entries.OfType<FileInfo>().Sum(file => file.Length);

            State = TaskState.Data;
            HandleEntries(entries);
        }

        /// <summary>
        /// Creates a recursive list of all filesystem entries in a certain directory sorted in C order.
        /// </summary>
        /// <param name="path">The path of the directory to analyze.</param>
        /// <returns>An array of filesystem entries.</returns>
        /// <exception cref="IOException">The directory could not be processed.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the directory is not permitted.</exception>
        private static FileSystemInfo[] GetSortedDirectoryEntries(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            // Get separated lists for files and directories
            var files = Directory.GetFiles(path);
            var directories = Directory.GetDirectories(path);

            // C-sort the lists
            Array.Sort(files, StringComparer.Ordinal);
            Array.Sort(directories, StringComparer.Ordinal);

            // Create the combined result list (files first, then sub-directories)
            var result = new List<FileSystemInfo>(files.Select(file => new FileInfo(file)));
            foreach (string directory in directories)
            {
                result.Add(new DirectoryInfo(directory));

                // Recurse into sub-directories (but do not follow symlinks)
                if (!FileUtils.IsSymlink(directory)) result.AddRange(GetSortedDirectoryEntries(directory));
            }
            return result.ToArray();
        }

        /// <summary>
        /// Iterates over all <paramref name="entries"/> and calls handler methods for them.
        /// </summary>
        /// <exception cref="NotSupportedException">A file has illegal properties (e.g. is a device file, has line breaks in the filename, etc.).</exception>
        /// <exception cref="IOException">There was an error reading a file.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to read a file.</exception>
        protected virtual void HandleEntries(IEnumerable<FileSystemInfo> entries)
        {
            var externalXbits = FlagUtils.GetFiles(FlagUtils.XbitFile, SourceDirectory.FullName);
            var externalSymlinks = FlagUtils.GetFiles(FlagUtils.SymlinkFile, SourceDirectory.FullName);

            foreach (var entry in entries ?? throw new ArgumentNullException(nameof(entries)))
            {
                CancellationToken.ThrowIfCancellationRequested();

                switch (entry)
                {
                    case FileInfo file when file.Name != Manifest.ManifestFile && file.Name != FlagUtils.XbitFile && file.Name != FlagUtils.SymlinkFile:
                        HandleEntry(file, externalXbits, externalSymlinks);
                        UnitsProcessed += file.Length;
                        break;
                    case DirectoryInfo directory:
                        HandleEntry(directory);
                        break;
                }
            }
        }

        /// <summary>
        /// Handles a file system entry the OS reports as a file.
        /// </summary>
        /// <param name="entry">The file entry to handle.</param>
        /// <param name="externalXbits">A list of fully qualified paths of files that are named in the <see cref="FlagUtils.SymlinkFile"/>.</param>
        /// <param name="externalSymlinks">A list of fully qualified paths of files that are named in the <see cref="FlagUtils.SymlinkFile"/>.</param>
        /// <exception cref="NotSupportedException">The <paramref name="entry"/> has illegal properties (e.g. is a device file, has line breaks in the filename, etc.).</exception>
        /// <exception cref="IOException">There was an error reading the file.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to read the file.</exception>
        private void HandleEntry(FileInfo entry, ICollection<string> externalXbits, ICollection<string> externalSymlinks)
        {
            if (_sourceIsUnixFS)
            {
                if (FileUtils.IsSymlink(entry.FullName, out string? symlinkTarget))
                    HandleSymlink(entry, symlinkTarget);
                else if (FileUtils.IsExecutable(entry.FullName))
                    HandleFile(entry, executable: true);
                else if (!FileUtils.IsRegularFile(entry.FullName))
                    throw new NotSupportedException(string.Format(Resources.IllegalFileType, entry.FullName));
                else HandleFile(entry);
            }
            else
            {
                if (CygwinUtils.IsSymlink(entry.FullName, out string? symlinkTarget))
                    HandleSymlink(entry, symlinkTarget);
                else if (externalSymlinks.Contains(entry.FullName))
                    HandleSymlink(entry, File.ReadAllText(entry.FullName, Encoding.UTF8));
                else if (externalXbits.Contains(entry.FullName))
                    HandleFile(entry, executable: true);
                else HandleFile(entry);
            }
        }

        /// <summary>
        /// Handles a file system entry the OS reports as a directory.
        /// </summary>
        /// <param name="entry">The directory entry to handles.</param>
        /// <exception cref="IOException">There was an error reading the directory.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to read the directory.</exception>
        private void HandleEntry(DirectoryInfo entry)
        {
            if (_sourceIsUnixFS && FileUtils.IsSymlink(entry.FullName, out string? target))
                HandleSymlink(entry, target);
            else
                HandleDirectory(entry);
        }

        /// <summary>
        /// Handles a file.
        /// </summary>
        /// <param name="file">The file to handle.</param>
        /// <param name="executable"><c>true</c> indicates that the file is marked as executable.</param>
        protected abstract void HandleFile(FileInfo file, bool executable = false);

        /// <summary>
        /// Handles a symlink.
        /// </summary>
        /// <param name="symlink">The symlink to handle.</param>
        /// <param name="target">The target the symlink points to.</param>
        protected abstract void HandleSymlink(FileSystemInfo symlink, string target);

        /// <summary>
        /// Handles a directory.
        /// </summary>
        /// <param name="directory">The directory to handle.</param>
        protected abstract void HandleDirectory(DirectoryInfo directory);
    }
}
