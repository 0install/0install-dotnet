// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Globalization;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// A symlink entry in a <see cref="Manifest"/>.
    /// </summary>
    /// <remarks>This class is immutable. It should only be used as a part of a <see cref="Manifest"/>.</remarks>
    [Serializable]
    public sealed class ManifestSymlink : ManifestDirectoryElement, IEquatable<ManifestSymlink>
    {
        /// <summary>
        /// Creates a new symlink-entry.
        /// </summary>
        /// <param name="digest">The digest of the link target path.</param>
        /// <param name="size">The length of the link target path.</param>
        /// <param name="name">The name of the symlink without the containing directory.</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> contains a newline character.</exception>
        internal ManifestSymlink(string digest, long size, string name)
            : base(digest, size, name)
        {}

        #region Factory methods
        /// <summary>
        /// Creates a new node from a string representation as created by <see cref="ToString"/>.
        /// </summary>
        /// <param name="line">The string representation to parse.</param>
        /// <returns>The newly created node.</returns>
        /// <exception cref="FormatException">The <paramref name="line"/> format is incorrect.</exception>
        internal static ManifestSymlink FromString(string line)
        {
            const int numberOfParts = 4;
            var parts = line.Split(new[] {' '}, numberOfParts);
            if (parts.Length != numberOfParts) throw new FormatException(Resources.InvalidNumberOfLineParts);

            try
            {
                return new ManifestSymlink(parts[1], long.Parse(parts[2]), parts[3]);
            }
            #region Error handling
            catch (OverflowException ex)
            {
                throw new FormatException(Resources.NumberTooLarge, ex);
            }
            #endregion
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the string representation of this node for the manifest format.
        /// </summary>
        /// <returns><c>"S", space, hash, space, size, space, symlink name, newline</c></returns>
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "S {0} {1} {2}", Digest, Size, Name);
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(ManifestSymlink other) => base.Equals(other);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is ManifestSymlink symlink && Equals(symlink);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => base.GetHashCode();
        #endregion
    }
}
