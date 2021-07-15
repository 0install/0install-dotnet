// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Text;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using ZeroInstall.Store.Implementations.Manifests;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Builds an implementation as an on-disk directory.
    /// </summary>
    public class DirectoryBuilder : IImplementationBuilder
    {
        private readonly string _path;
        private readonly IImplementationBuilder? _innerBuilder;

        /// <summary>
        /// A directory all hardlink targets must be a child of.
        /// </summary>
        public string HardlinksMustBeChildOf { get; init; }

        /// <summary>
        /// Creates a new implementation builder.
        /// </summary>
        /// <param name="path">The path to the directory to build the implementation in.</param>
        /// <param name="innerBuilder">An additional <see cref="IImplementationBuilder"/> to pass all calls on to as well. Usually <see cref="Manifests.ManifestBuilder"/>.</param>
        public DirectoryBuilder(string path, IImplementationBuilder? innerBuilder = null)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _innerBuilder = innerBuilder;
            HardlinksMustBeChildOf = path;
        }

        /// <inheritdoc/>
        public void AddDirectory(string path)
            => Directory.CreateDirectory(GetFullPath(path));

        /// <inheritdoc/>
        public void AddFile(string path, Stream stream, UnixTime modifiedTime, bool executable = false)
        {
            string fullPath = GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            // Delete any preexisting file to reset permissions, etc.
            if (File.Exists(fullPath)) File.Delete(fullPath);

            using (var fileStream = File.Create(fullPath))
            {
                if (_innerBuilder == null)
                    stream.CopyToEx(fileStream);
                else
                    _innerBuilder.AddFile(path, new ShadowingStream(stream, fileStream), modifiedTime, executable);
            }

            if (executable && UnixUtils.IsUnix)
                FileUtils.SetExecutable(fullPath, true);
            File.SetLastWriteTimeUtc(fullPath, modifiedTime);
        }

        /// <inheritdoc/>
        public void AddHardlink(string path, string target, bool executable = false)
        {
            string sourceAbsolute = GetFullPath(path);
            string targetAbsolute = GetFullPath(target, HardlinksMustBeChildOf);
            Directory.CreateDirectory(Path.GetDirectoryName(targetAbsolute)!);

            try
            {
                FileUtils.CreateHardlink(sourceAbsolute, targetAbsolute);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(targetAbsolute, sourceAbsolute);
            }
            catch (UnauthorizedAccessException)
            {
                File.Copy(targetAbsolute, sourceAbsolute);
            }

            if (executable)
                FileUtils.SetExecutable(targetAbsolute, true);

            _innerBuilder?.AddHardlink(path, target, executable);
        }

        /// <inheritdoc/>
        public void AddSymlink(string path, string target)
        {
            string sourceAbsolute = GetFullPath(path);

            // Delete any preexisting file to reset permissions, etc.
            if (File.Exists(sourceAbsolute)) File.Delete(sourceAbsolute);

            try
            {
                FileUtils.CreateSymlink(sourceAbsolute, target);
            }
            catch (IOException) when (WindowsUtils.IsWindows)
            { // NTFS symbolic links require admin privileges; use Cygwin symlinks instead
                CygwinUtils.CreateSymlink(sourceAbsolute, target);
            }

            _innerBuilder?.AddSymlink(path, target);
        }

        /// <inheritdoc/>
        public void Rename(string path, string target)
        {
            string fullSourcePath = GetFullPath(path);
            string fullTargetPath = GetFullPath(target);
            Directory.CreateDirectory(Path.GetDirectoryName(fullTargetPath)!);

            if (File.Exists(fullSourcePath))
                File.Move(fullSourcePath, fullTargetPath);
            else if (Directory.Exists(fullSourcePath))
                Directory.Move(fullSourcePath, fullTargetPath);
            else throw new IOException(string.Format(Resources.FileOrDirNotFound, path));

            _innerBuilder?.Rename(path, target);
        }

        /// <inheritdoc/>
        public void Remove(string path)
        {
            string fullPath = GetFullPath(path);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            else if (Directory.Exists(fullPath))
                Directory.Delete(fullPath, recursive: true);
            else throw new IOException(string.Format(Resources.FileOrDirNotFound, path));

            _innerBuilder?.Remove(path);
        }

        /// <inheritdoc/>
        public void MarkAsExecutable(string path)
        {
            string fullPath = GetFullPath(path);

            if (!UnixUtils.IsUnix) return;

            var modifiedTime = File.GetLastWriteTimeUtc(fullPath);
            FileUtils.SetExecutable(fullPath, true);
            File.SetLastWriteTimeUtc(fullPath, modifiedTime);
        }

        /// <inheritdoc/>
        public void TurnIntoSymlink(string path)
        {
            string fullPath = GetFullPath(path);

            if (!FileUtils.IsSymlink(fullPath))
                AddSymlink(path, File.ReadAllText(fullPath, Encoding.UTF8));
        }

        /// <summary>
        /// Resolves a path relative to <see cref="_path"/> to a full path.
        /// </summary>
        /// <param name="relativePath">The relative path to resolve.</param>
        /// <param name="mustBeChildOf">A directory the resulting path must be a child of. Defaults to <see cref="_path"/>.</param>
        /// <exception cref="IOException"><paramref name="relativePath"/> is invalid (e.g. is absolute, points outside the archive's root, contains invalid characters).</exception>
        private string GetFullPath(string relativePath, string? mustBeChildOf = null)
        {
            if (Manifest.IsReservedName(relativePath)) throw new IOException("Reserved name"); // TODO: Localize

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Path.Combine(_path, relativePath));
            }
            #region Error handling
            catch (ArgumentException ex)
            {
                throw new IOException(ex.Message, ex);
            }
            #endregion

            if (!fullPath.StartsWith((mustBeChildOf ?? _path) + Path.DirectorySeparatorChar))
                throw new IOException(string.Format("The path '{0}' points out of the implementation directory", relativePath)); // TODO: Localize

            return fullPath;
        }
    }
}
