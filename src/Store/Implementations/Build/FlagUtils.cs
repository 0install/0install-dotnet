// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Some file flags (executable, symlink, etc.) cannot be stored directly as filesystem attributes on some platforms (e.g. Windows). They can be kept track of in external "flag files" instead.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flag")]
    public static class FlagUtils
    {
        /// <summary>
        /// The well-known file name used for a flag file to indicate that a directory resides on a non-Unix filesystem.
        /// </summary>
        public const string NoUnixFSFile = ".no-unix-fs";

        /// <summary>
        /// The well-known file name used to store executable flags in directories.
        /// </summary>
        public const string XbitFile = ".xbit";

        /// <summary>
        /// The well-known file name used to store symlink flags in directories.
        /// </summary>
        public const string SymlinkFile = ".symlink";

        #region Read
        /// <summary>
        /// Determines whether a directory resides on a non-Unix filesystem.
        /// </summary>
        /// <param name="directoryPath">The full path to the directory.</param>
        /// <remarks>The flag file is searched for instead of specifying it directly to allow handling of special cases like creating manifests of subdirectories of extracted archives.</remarks>
        /// <seealso cref="NoUnixFSFile"/>
        /// <seealso cref="FileUtils.IsUnixFS"/>
        public static bool IsUnixFS(string directoryPath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(directoryPath)) throw new ArgumentNullException(nameof(directoryPath));
            #endregion

            // Move up one level to avoid write-protection within implementation directories
            if (ImplementationStoreUtils.IsImplementation(directoryPath, out string? implementationPath))
                directoryPath = Path.Combine(implementationPath, "..");

            try
            {
                return FindRootDir(NoUnixFSFile, directoryPath) == null
                    && FileUtils.IsUnixFS(directoryPath);
            }
            #region Error handling
            catch (IOException)
            {
                // Just assume the target is a Unix FS if the check fails on a Unixoid OS
                return UnixUtils.IsUnix;
            }
            catch (UnauthorizedAccessException)
            {
                // Just assume the target is a Unix FS if the check fails on a Unixoid OS
                return UnixUtils.IsUnix;
            }
            #endregion
        }

        /// <summary>
        /// Retrieves a list of files for which an external flag is set.
        /// </summary>
        /// <param name="flagName">The name of the flag type to search for (<see cref="XbitFile"/> or <see cref="SymlinkFile"/>).</param>
        /// <param name="directoryPath">The target directory to start the search from (will go upwards through directory levels one-by-one, thus may deliver "too many" results).</param>
        /// <returns>A list of fully qualified paths of files that are named in an external flag file.</returns>
        /// <exception cref="IOException">There was an error reading the flag file.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to read the flag file.</exception>
        /// <remarks>The flag file is searched for instead of specifying it directly to allow handling of special cases like creating manifests of subdirectories of extracted archives.</remarks>
        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "flag")]
        public static ICollection<string> GetFiles(string flagName, string directoryPath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(flagName)) throw new ArgumentNullException(nameof(flagName));
            if (string.IsNullOrEmpty(directoryPath)) throw new ArgumentNullException(nameof(directoryPath));
            #endregion

            string? flagDir = FindRootDir(flagName, directoryPath);
            if (flagDir == null) return Array.Empty<string>();

            string path = Path.Combine(flagDir, flagName);
            using (new AtomicRead(path))
            {
                using var reader = File.OpenText(path);
                var externalFlags = new HashSet<string>();

                // Each line in the file signals a flagged file
                while (!reader.EndOfStream)
                {
                    string? line = reader.ReadLine();
                    if (line != null && line.StartsWith("/"))
                    {
                        // Trim away the first slash and then replace Unix-style slashes
                        string relativePath = FileUtils.UnifySlashes(line[1..]);
                        externalFlags.Add(Path.Combine(flagDir, relativePath));
                    }
                }

                return externalFlags;
            }
        }

        /// <summary>
        /// Determines whether an external flag is set for a specific file. Use <see cref="GetFiles"/> instead when possible for better performance.
        /// </summary>
        /// <param name="flagName">The name of the flag type to search for (<see cref="XbitFile"/> or <see cref="SymlinkFile"/>).</param>
        /// <param name="filePath">The absolute path of file to check.</param>
        /// <returns>A list of fully qualified paths of files that are named in an external flag file.</returns>
        /// <exception cref="ArgumentException"><paramref name="filePath"/> is not an absolute path.</exception>
        /// <exception cref="IOException">There was an error reading the flag file.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to read the flag file.</exception>
        /// <remarks>The flag file is searched for instead of specifying it directly to allow handling of special cases like creating manifests of subdirectories of extracted archives.</remarks>
        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "flag")]
        public static bool IsFlagged(string flagName, string filePath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(flagName)) throw new ArgumentNullException(nameof(flagName));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!Path.IsPathRooted(filePath)) throw new ArgumentException($"'{filePath}' is not an absolute path.", nameof(filePath));
            #endregion

            string directoryPath = Path.GetDirectoryName(filePath)!;
            if (directoryPath == null) throw new ArgumentException($"'{filePath}' is not an absolute path.", nameof(filePath));
            return GetFiles(flagName, directoryPath).Contains(filePath);
        }

        /// <summary>
        /// Searches for a flag file starting in the <paramref name="directoryPath"/> directory and moving upwards until it finds it or until it reaches the root directory.
        /// </summary>
        /// <param name="flagName">The name of the flag type to search for (<see cref="XbitFile"/> or <see cref="SymlinkFile"/>).</param>
        /// <param name="directoryPath">The target directory to start the search from.</param>
        /// <returns>The full path to the closest flag file that was found; <c>null</c> if none was found.</returns>
        private static string? FindRootDir(string flagName, string directoryPath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(flagName)) throw new ArgumentNullException(nameof(flagName));
            if (string.IsNullOrEmpty(directoryPath)) throw new ArgumentNullException(nameof(directoryPath));
            #endregion

            // Start searching for the flag file in the target directory and then move upwards
            string? flagDir = Path.GetFullPath(directoryPath);
            while (!File.Exists(Path.Combine(flagDir, flagName)))
            {
                // Go up one level in the directory hierarchy
                flagDir = Path.GetDirectoryName(flagDir);

                // Cancel once the root dir has been reached
                if (flagDir == null) break;
            }

            return flagDir;
        }
        #endregion

        #region Write
        /// <summary>
        /// Sets a flag for a directory indicating that it resides on a non-Unix filesystem. This makes future calls to <see cref="IsUnixFS"/> run faster and more reliable.
        /// </summary>
        /// <param name="directoryPath">The full path to the directory.</param>
        /// <exception cref="IOException">There was an error writing the flag file.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to write the flag file.</exception>
        /// <seealso cref="NoUnixFSFile"/>
        public static void MarkAsNoUnixFS(string directoryPath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(directoryPath)) throw new ArgumentNullException(nameof(directoryPath));
            #endregion

            FileUtils.Touch(Path.Combine(directoryPath, NoUnixFSFile));
        }

        /// <summary>
        /// Sets a flag for a file in an external flag file.
        /// </summary>
        /// <param name="path">The full path to the flag file, named <see cref="XbitFile"/> or <see cref="SymlinkFile"/>.</param>
        /// <param name="relativePath">The path of the file to set relative to <paramref name="path"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="relativePath"/> is not a relative path.</exception>
        /// <exception cref="IOException">There was an error writing the flag file.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to write the flag file.</exception>
        public static void Set(string path, string relativePath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(relativePath)) throw new ArgumentNullException(nameof(relativePath));
            if (Path.IsPathRooted(relativePath)) throw new ArgumentException(Resources.PathNotRelative, nameof(relativePath));
            #endregion

            // Convert path to rooted Unix-style
            string unixPath = "/" + relativePath.Replace(Path.DirectorySeparatorChar, '/');

            using var flagFile = new StreamWriter(path, append: true, encoding: FeedUtils.Encoding) {NewLine = "\n"};
            flagFile.WriteLine(unixPath);
        }

        /// <summary>
        /// Sets a flag for a file in an external flag file in an automatically chosen location. Use <see cref="Set"/> instead when possible for predictable flag file locations.
        /// </summary>
        /// <param name="flagName">The name of the flag type to set (<see cref="XbitFile"/> or <see cref="SymlinkFile"/>).</param>
        /// <param name="filePath">The absolute path of file to set the flag for.</param>
        /// <exception cref="ArgumentException"><paramref name="filePath"/> is not an absolute path.</exception>
        /// <exception cref="IOException">There was an error writing the flag file.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to write the flag file.</exception>
        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "flag")]
        public static void SetAuto(string flagName, string filePath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(flagName)) throw new ArgumentNullException(nameof(flagName));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!Path.IsPathRooted(filePath)) throw new ArgumentException($"'{filePath}' is not an absolute path.", nameof(filePath));
            #endregion

            string directoryPath = Path.GetDirectoryName(filePath)!;
            if (directoryPath == null) throw new ArgumentException($"'{filePath}' is not an absolute path.", nameof(filePath));
            string flagDir = FindRootDir(flagName, directoryPath) ?? directoryPath;

            Set(
                Path.Combine(flagDir, flagName),
                new FileInfo(filePath).RelativeTo(new DirectoryInfo(flagDir)));
        }

        /// <summary>
        /// Removes one or more flags for a file or directory in an external flag file.
        /// </summary>
        /// <param name="path">The full path to the flag file, named <see cref="XbitFile"/> or <see cref="SymlinkFile"/>.</param>
        /// <param name="relativePath">The path of the file or directory to remove relative to <paramref name="path"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="relativePath"/> is not a relative path.</exception>
        /// <exception cref="IOException">There was an error writing the flag file.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to write the flag file.</exception>
        public static void Remove(string path, string relativePath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(relativePath)) throw new ArgumentNullException(nameof(relativePath));
            if (Path.IsPathRooted(relativePath)) throw new ArgumentException(Resources.PathNotRelative, nameof(relativePath));
            #endregion

            if (!File.Exists(path)) return;

            // Convert path to rooted Unix-style
            string unixPath = "/" + relativePath.Replace(Path.DirectorySeparatorChar, '/');

            using var atomic = new AtomicWrite(path);
            using var newFlagFile = new StreamWriter(atomic.WritePath, append: false, encoding: FeedUtils.Encoding) {NewLine = "\n"};
            using var oldFlagFile = File.OpenText(path);
            // Each line in the file signals a flagged file
            while (!oldFlagFile.EndOfStream)
            {
                string? line = oldFlagFile.ReadLine();
                if (line != null && line.StartsWith("/"))
                {
                    if (line == unixPath || line.StartsWith(unixPath + "/")) continue; // Filter out removed files

                    newFlagFile.WriteLine(line);
                }
            }
            atomic.Commit();
        }

        /// <summary>
        /// Adds a directory prefix to all entries in an external flag file.
        /// </summary>
        /// <param name="path">The full path to the flag file, named <see cref="XbitFile"/> or <see cref="SymlinkFile"/>.</param>
        /// <param name="source">The old path of the renamed file or directory relative to <paramref name="path"/>.</param>
        /// <param name="destination">The new path of the renamed file or directory relative to <paramref name="path"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="destination"/> is not a relative path.</exception>
        /// <exception cref="IOException">There was an error writing the flag file.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to write the flag file.</exception>
        public static void Rename(string path, string source, string destination)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(source)) throw new ArgumentNullException(nameof(source));
            if (Path.IsPathRooted(source)) throw new ArgumentException(Resources.PathNotRelative, nameof(source));
            if (string.IsNullOrEmpty(destination)) throw new ArgumentNullException(nameof(destination));
            if (Path.IsPathRooted(destination)) throw new ArgumentException(Resources.PathNotRelative, nameof(destination));
            #endregion

            if (!File.Exists(path)) return;

            // Convert paths to rooted Unix-style
            source = "/" + source.Replace(Path.DirectorySeparatorChar, '/');
            destination = "/" + destination.Replace(Path.DirectorySeparatorChar, '/');

            using var atomic = new AtomicWrite(path);
            using var newFlagFile = new StreamWriter(atomic.WritePath, append: false, encoding: FeedUtils.Encoding) {NewLine = "\n"};
            using var oldFlagFile = File.OpenText(path);
            // Each line in the file signals a flagged file
            while (!oldFlagFile.EndOfStream)
            {
                string? line = oldFlagFile.ReadLine();
                if (line != null && line.StartsWith("/"))
                {
                    if (line == source || line.StartsWith(source + "/"))
                        newFlagFile.WriteLine(destination + line[source.Length..]);
                    else newFlagFile.WriteLine(line);
                }
            }
            atomic.Commit();
        }
        #endregion

        #region Convert
        /// <summary>
        /// Converts all flag files in a directory into real filesystem attributes (executable bits and symlinks).
        /// </summary>
        /// <param name="path">The path to the directory to convert.</param>
        public static void ConvertToFS(string path)
        {
            string xbitFile = Path.Combine(path, XbitFile);
            if (File.Exists(xbitFile))
            {
                foreach (string file in GetFiles(XbitFile, path))
                    FileUtils.SetExecutable(file, executable: true);

                File.Delete(xbitFile);
            }

            string symlinkFile = Path.Combine(path, SymlinkFile);
            if (File.Exists(xbitFile))
            {
                foreach (string file in GetFiles(SymlinkFile, path))
                {
                    string linkDestination = File.ReadAllText(file);
                    File.Delete(file);
                    FileUtils.CreateSymlink(file, linkDestination);
                }
                File.Delete(symlinkFile);
            }
        }
        #endregion
    }
}
