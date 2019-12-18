// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using JetBrains.Annotations;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// An abstract base class for directory-element entries (files and symlinks) in a <see cref="Manifest"/>.
    /// </summary>
    /// <remarks>This class and the derived classes are immutable. They should only be used as a part of a <see cref="Manifest"/>.</remarks>
    [Serializable]
    public abstract class ManifestDirectoryElement : ManifestNode
    {
        /// <summary>
        /// The digest of the content of the file calculated using the selected digest algorithm.
        /// </summary>
        [NotNull]
        public string Digest { get; }

        /// <summary>
        /// The size of the file in bytes.
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// The name of the file without the containing directory.
        /// </summary>
        [NotNull]
        public string Name { get; }

        /// <summary>
        /// Creates a new directory element entry.
        /// </summary>
        /// <param name="digest">The digest of the content of the element calculated using the selected digest algorithm.</param>
        /// <param name="size">The size of the element in bytes.</param>
        /// <param name="name">The name of the element without the containing directory.</param>
        /// <exception cref="NotSupportedException"><paramref name="name"/> contains a newline character.</exception>
        protected ManifestDirectoryElement(string digest, long size, string name)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(digest)) throw new ArgumentNullException(nameof(digest));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (name.Contains("\n")) throw new NotSupportedException(Resources.NewlineInName);
            #endregion

            Digest = digest;
            Size = size;
            Name = name;
        }

        #region Equality
        protected bool Equals(ManifestDirectoryElement other) => other != null && other.Digest == Digest && other.Size == Size && other.Name == Name;

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(Digest, Size, Name);
        #endregion
    }
}
