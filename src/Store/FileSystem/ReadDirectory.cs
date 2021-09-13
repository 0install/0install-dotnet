// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Manifests;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.FileSystem
{
    /// <summary>
    /// Reads the content of a directory to an <see cref="IBuilder"/>.
    /// </summary>
    public class ReadDirectory : ReadDirectoryBase
    {
        private readonly IForwardOnlyBuilder _builder;
        private readonly Manifest? _manifest;

        /// <summary>
        /// Creates a new directory read task.
        /// </summary>
        /// <param name="path">The path of the directory to read.</param>
        /// <param name="builder">The builder to read to.</param>
        public ReadDirectory(string path, IForwardOnlyBuilder builder)
            : base(path)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));

            string manifestPath = Path.Combine(path, Manifest.ManifestFile);

            bool ShouldReadManifest()
            {
                try
                {
                    // Symlinks and executable bits may be lost on non-Unix filesystems and can be reconstructed from a manifest file
                    return !FileUtils.IsUnixFS(Path.Combine(path, ".."));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return true;
                }
            }

            if (File.Exists(manifestPath) && ShouldReadManifest())
                _manifest = Manifest.Load(manifestPath, ManifestFormat.Sha1New);
        }

        /// <inheritdoc/>
        public override string Name => Resources.ProcessingFiles;

        /// <inheritdoc/>
        protected override void HandleDirectory(DirectoryInfo entry)
        {
            if (entry.IsSymlink(out string? target))
                _builder.AddSymlink(entry.RelativeTo(Source), target);
            else
                _builder.AddDirectory(entry.RelativeTo(Source));
        }

        /// <inheritdoc/>
        protected override void HandleFile(FileInfo file, FileInfo? hardlinkTarget = null)
        {
            if (Manifest.RejectPath(file.RelativeTo(Source))) return;

            var element = GetManifestElement(file);

            if (ImplFileUtils.IsSymlink(file.FullName, out string? symlinkTarget, element))
            {
                _builder.AddSymlink(file.RelativeTo(Source), symlinkTarget);
                return;
            }

            if (!FileUtils.IsRegularFile(file.FullName))
                throw new NotSupportedException(string.Format(Resources.IllegalFileType, file.FullName));

            bool executable = ImplFileUtils.IsExecutable(file.FullName, element);
            try
            {
                if (hardlinkTarget != null)
                {
                    _builder.AddHardlink(file.RelativeTo(Source), hardlinkTarget.RelativeTo(Source), executable);
                    return;
                }
            }
            catch (NotSupportedException)
            {}

            using var stream = file.OpenRead();
            _builder.AddFile(file.RelativeTo(Source), stream, file.LastWriteTimeUtc, executable);
        }

        /// <summary>
        /// Tries to get a <paramref name="file"/>'s equivalent entry in the <see cref="_manifest"/>.
        /// </summary>
        private ManifestElement? GetManifestElement(FileInfo file)
            => _manifest != null
            && _manifest.TryGetValue(file.Directory!.RelativeTo(Source).ToUnixPath(), out var dir)
            && dir.TryGetValue(file.Name, out var element)
                ? element
                : null;
    }
}
