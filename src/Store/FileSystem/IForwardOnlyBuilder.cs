// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common;

namespace ZeroInstall.Store.FileSystem
{
    /// <summary>
    /// Builds a file system directory without changing or removing elements that have already been added.
    /// </summary>
    public interface IForwardOnlyBuilder
    {
        /// <summary>
        /// Adds a subdirectory to the implementation.
        /// </summary>
        /// <param name="path">The path of the directory to create relative to the implementation root.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        void AddDirectory(string path);

        /// <summary>
        /// Adds a file to the implementation.
        /// </summary>
        /// <param name="path">The path of the file to create relative to the implementation root.</param>
        /// <param name="stream">The contents of the file.</param>
        /// <param name="modifiedTime">The last write time to set for the file.</param>
        /// <param name="executable"><c>true</c> if the file's executable bit is to be set; <c>false</c> otherwise.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        void AddFile(string path, Stream stream, UnixTime modifiedTime, bool executable = false);

        /// <summary>
        /// Adds a symbolic link to the implementation.
        /// </summary>
        /// <param name="path">The path of the symlink to create relative to the implementation root.</param>
        /// <param name="target">The target the symbolic link shall point to relative to <paramref name="path"/>. May use non-native path separators.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        void AddSymlink(string path, string target);

        /// <summary>
        /// Adds a hardlink to the implementation.
        /// </summary>
        /// <param name="path">The path of the hardlink to create relative to the implementation root.</param>
        /// <param name="target">The path of the existing file the hardlink shall be based on relative to the implementation root. Must point</param>
        /// <param name="executable"><c>true</c> if the executable bit of the hardlink is set; <c>false</c> otherwise.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        /// <exception cref="NotSupportedException">The currently platform or builder does not support hardlinks. Use <see cref="AddFile"/> instead.</exception>
        void AddHardlink(string path, string target, bool executable = false);
    }
}
