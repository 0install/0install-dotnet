// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Copies the content of a single file to a new location preserving the original file modification time, executable bit and/or symlink status.
    /// </summary>
    public class CloneFile : CloneDirectory
    {
        /// <summary>
        /// The name of the original file to read without any directory information.
        /// </summary>
        public string SourceFileName { get; }

        /// <summary>
        /// The name of the new file to write without any directory information.
        /// </summary>
        public string TargetFileName { get; set; }

        /// <summary>
        /// Creates a new file cloning task.
        /// </summary>
        /// <param name="sourceFilePath">The path of the original file to read.</param>
        /// <param name="targetDirPath">The path of the new directory to clone the file to.</param>
        public CloneFile(string sourceFilePath, string targetDirPath)
            : base(
                sourcePath: Path.GetDirectoryName(sourceFilePath) ?? throw new ArgumentNullException(sourceFilePath),
                targetPath: targetDirPath ?? throw new ArgumentNullException(targetDirPath))
        {
            SourceFileName = Path.GetFileName(sourceFilePath);
            TargetFileName = SourceFileName;
        }

        /// <inheritdoc/>
        protected override void HandleFile(FileInfo file, bool executable = false)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (file.Name == SourceFileName) base.HandleFile(file, executable);
        }

        /// <inheritdoc/>
        protected override void HandleSymlink(FileSystemInfo symlink, string target)
        {
            if (symlink == null) throw new ArgumentNullException(nameof(symlink));
            if (string.IsNullOrEmpty(target)) throw new ArgumentNullException(nameof(target));
            if (symlink.Name == SourceFileName) DirectoryBuilder.CreateSymlink(TargetFileName, target);
        }

        /// <inheritdoc/>
        protected override string NewFilePath(FileInfo originalFile, DateTime? lastWriteTime, bool executable)
            => DirectoryBuilder.NewFilePath(TargetFileName, lastWriteTime, executable);
    }
}
