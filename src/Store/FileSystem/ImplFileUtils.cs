// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.FileSystem
{
    /// <summary>
    /// Provides filesystem helper methods for working with implementation directories.
    /// </summary>
    public static class ImplFileUtils
    {
        /// <summary>
        /// The name of the NTFS alternate stream used to mark a file as Unix-executable.
        /// </summary>
        private const string ExecutableIndicator = "xbit";

        /// <summary>
        /// Checks whether a file is marked as Unix-executable.
        /// </summary>
        /// <param name="path">The path of the file to check.</param>
        /// <param name="manifestElement">The file's equivalent manifest entry, if available.</param>
        /// <returns><c>true</c> if <paramref name="path"/> points to an executable; <c>false</c> otherwise.</returns>
        public static bool IsExecutable(string path, ManifestElement? manifestElement = null)
        {
            if (manifestElement != null)
                return manifestElement is ManifestExecutableFile;
            if (FileUtils.IsExecutable(path))
                return true;
            if (WindowsUtils.IsWindowsNT && FileUtils.ReadExtendedMetadata(path, ExecutableIndicator) != null)
                return true;

            return false;
        }

        /// <summary>
        /// Marks a file as Unix-executable.
        /// </summary>
        /// <param name="fullPath">The absolute path of the file.</param>
        public static void SetExecutable(string fullPath)
        {
            var modifiedTime = File.GetLastWriteTimeUtc(fullPath);

            if (UnixUtils.IsUnix)
                FileUtils.SetExecutable(fullPath, true);
            else if (WindowsUtils.IsWindowsNT)
                FileUtils.WriteExtendedMetadata(fullPath, ExecutableIndicator, Array.Empty<byte>());

            File.SetLastWriteTimeUtc(fullPath, modifiedTime);
        }

        /// <summary>
        /// Checks whether a file is a symbolic link.
        /// </summary>
        /// <param name="path">The path of the file to check.</param>
        /// <param name="manifestElement">The file's equivalent manifest entry, if available.</param>
        /// <param name="target">Returns the target the symbolic link points to if it exists.</param>
        /// <returns><c>true</c> if <paramref name="manifestElement"/> points to a symbolic link; <c>false</c> otherwise.</returns>
        public static bool IsSymlink(string path, [NotNullWhen(true)] out string? target, ManifestElement? manifestElement = null)
        {
            if (FileUtils.IsSymlink(path, out target) || CygwinUtils.IsSymlink(path, out target))
                return true;

            if (manifestElement is ManifestSymlink)
            {
                target = File.ReadAllText(path, Encoding.UTF8);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a new symbolic link to a file or directory.
        /// </summary>
        /// <param name="sourcePath">The path of the link to create.</param>
        /// <param name="targetPath">The path of the existing file or directory to point to (relative to <paramref name="sourcePath" />).</param>
        public static void CreateSymlink(string sourcePath, string targetPath)
        {
            try
            {
                FileUtils.CreateSymlink(sourcePath, targetPath);
            }
            catch (IOException) when (WindowsUtils.IsWindows)
            {
                Log.Debug("Creating Cygwin symlink instead of NTFS symlink due to insufficient permissions: " + sourcePath);
                CygwinUtils.CreateSymlink(sourcePath, targetPath);
            }
        }
    }
}
