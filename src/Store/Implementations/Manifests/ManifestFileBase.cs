// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common.Storage;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// An abstract base class for file entries in a <see cref="Manifest"/>.
    /// </summary>
    /// <remarks>This class and the derived classes are immutable. They should only be used as a part of a <see cref="Manifest"/>.</remarks>
    [Serializable]
    public abstract class ManifestFileBase : ManifestDirectoryElement
    {
        /// <summary>
        /// The time this file was last modified as Unix time.
        /// </summary>
        protected long ModifiedTimeUnix { get; }

        /// <summary>
        /// The time this file was last modified.
        /// </summary>
        public DateTime ModifiedTime => FileUtils.FromUnixTime(ModifiedTimeUnix);

        /// <summary>
        /// Creates a new file entry.
        /// </summary>
        /// <param name="digest">The digest of the content of the file calculated using the selected digest algorithm.</param>
        /// <param name="modifiedTime">The time this file was last modified.</param>
        /// <param name="size">The size of the file in bytes.</param>
        /// <param name="name">The name of the file without the containing directory.</param>
        /// <exception cref="NotSupportedException"><paramref name="name"/> contains a newline character.</exception>
        protected ManifestFileBase(string digest, DateTime modifiedTime, long size, string name)
            : base(digest, size, name)
        {
            ModifiedTimeUnix = modifiedTime.ToUnixTime();
        }

        #region Equality
        protected bool Equals(ManifestFileBase other) => other != null && ModifiedTimeUnix == other.ModifiedTimeUnix && base.Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), ModifiedTimeUnix);
        #endregion
    }
}
