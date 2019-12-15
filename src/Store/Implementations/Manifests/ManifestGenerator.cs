// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Implementations.Build;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// Generates a <see cref="Manifests.Manifest"/> for a directory.
    /// </summary>
    public class ManifestGenerator : DirectoryTaskBase
    {
        /// <inheritdoc/>
        public override string Name => string.Format(Resources.GeneratingManifest, Format);

        /// <summary>
        /// The format of the manifest to generate.
        /// </summary>
        [NotNull]
        public ManifestFormat Format { get; }

        private readonly List<ManifestNode> _nodes = new List<ManifestNode>();

        /// <summary>
        /// If <see cref="TaskBase.State"/> is <see cref="TaskState.Complete"/> this property contains the generated <see cref="Manifests.Manifest"/>; otherwise it's <c>null</c>.
        /// </summary>
        public Manifest Manifest => new Manifest(Format, _nodes);

        /// <summary>
        /// Prepares to generate a manifest for a directory in the filesystem.
        /// </summary>
        /// <param name="sourcePath">The path of the directory to analyze.</param>
        /// <param name="format">The format of the manifest to generate.</param>
        public ManifestGenerator([NotNull] string sourcePath, [NotNull] ManifestFormat format)
            : base(sourcePath)
        {
            Format = format ?? throw new ArgumentNullException(nameof(format));
        }

        /// <inheritdoc/>
        protected override void HandleFile(FileInfo file, bool executable = false)
        {
            #region Sanity checks
            if (file == null) throw new ArgumentNullException(nameof(file));
            #endregion

            using var stream = file.OpenRead();
            if (executable) _nodes.Add(new ManifestExecutableFile(Format.DigestContent(stream), file.LastWriteTimeUtc, file.Length, file.Name));
            else _nodes.Add(new ManifestNormalFile(Format.DigestContent(stream), file.LastWriteTimeUtc, file.Length, file.Name));
        }

        /// <inheritdoc/>
        protected override void HandleSymlink(FileSystemInfo symlink, string target)
        {
            #region Sanity checks
            if (symlink == null) throw new ArgumentNullException(nameof(symlink));
            if (target == null) throw new ArgumentNullException(nameof(target));
            #endregion

            var data = target.ToStream();
            _nodes.Add(new ManifestSymlink(Format.DigestContent(data), data.Length, symlink.Name));
        }

        /// <inheritdoc/>
        protected override void HandleDirectory(DirectoryInfo directory)
        {
            #region Sanity checks
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            #endregion

            _nodes.Add(new ManifestDirectory("/" + directory.RelativeTo(SourceDirectory)));
        }
    }
}
