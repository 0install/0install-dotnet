// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Implementations.Build;
using ZeroInstall.Store.Implementations.Manifests;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Reads the content of a directory to an <see cref="IImplementationBuilder"/>.
    /// </summary>
    public class ReadDirectory : DirectoryTaskBase
    {
        private readonly IForwardOnlyImplementationBuilder _builder;
        private readonly Manifest? _manifest;

        /// <summary>
        /// Creates a new directory read task.
        /// </summary>
        /// <param name="path">The path of the directory to read.</param>
        /// <param name="builder">The implementation builder to read to.</param>
        /// <param name="manifest">Additional information source determining whether files should be treated as executable or as symlinks.</param>
        public ReadDirectory(string path, IForwardOnlyImplementationBuilder builder, Manifest? manifest = null)
            : base(path)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _manifest = manifest;
        }

        /// <inheritdoc/>
        public override string Name => "Reading files"; // TODO: Localize

        /// <inheritdoc/>
        protected override void HandleDirectory(DirectoryInfo directory)
            => _builder.AddDirectory(directory.RelativeTo(SourceDirectory));


        private readonly List<FileInfo> _previousFiles = new();

        /// <inheritdoc/>
        protected override void HandleFile(FileInfo file, bool executable = false)
        {
            if (file.DirectoryName == SourceDirectory.FullName && Manifest.IsReservedName(file.Name)) return;

            if (_manifest != null
             && _manifest.TryGetValue(file.Directory!.RelativeTo(SourceDirectory), out var dir)
             && dir.TryGetValue(file.Name, out var element))
            {
                if (element is ManifestSymlink)
                {
                    HandleSymlink(file, File.ReadAllText(file.FullName, Encoding.UTF8));
                    return;
                }
                executable |= element is ManifestExecutableFile;
            }

            var hardlinkTarget = _previousFiles.FirstOrDefault(previousFile => FileUtils.AreHardlinked(previousFile.FullName, file.FullName));
            if (hardlinkTarget == null)
            {
                _previousFiles.Add(file);
                using var stream = file.OpenRead();
                _builder.AddFile(file.RelativeTo(SourceDirectory), stream, file.LastWriteTimeUtc, executable);
            }
            else
                _builder.AddHardlink(file.RelativeTo(SourceDirectory), hardlinkTarget.RelativeTo(SourceDirectory), executable);
        }

        /// <inheritdoc/>
        protected override void HandleSymlink(FileSystemInfo symlink, string target)
            => _builder.AddSymlink(symlink.RelativeTo(SourceDirectory), target);
    }
}
