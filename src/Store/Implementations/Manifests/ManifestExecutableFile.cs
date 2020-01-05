// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Globalization;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// An executable file entry in a <see cref="Manifest"/>.
    /// </summary>
    /// <remarks>This class is immutable. It should only be used as a part of a <see cref="Manifest"/>.</remarks>
    [Serializable]
    public sealed class ManifestExecutableFile : ManifestFileBase, IEquatable<ManifestExecutableFile>
    {
        /// <summary>
        /// Creates a new executable file entry.
        /// </summary>
        /// <param name="hash">The hash of the content of the file calculated using the selected digest algorithm.</param>
        /// <param name="modifiedTime">The time this file was last modified.</param>
        /// <param name="size">The size of the file in bytes.</param>
        /// <param name="name">The name of the file without the containing directory.</param>
        /// <exception cref="NotSupportedException"><paramref name="name"/> contains a newline character.</exception>
        public ManifestExecutableFile(string hash, DateTime modifiedTime, long size, string name)
            : base(hash, modifiedTime, size, name)
        {}

        #region Factory methods
        /// <summary>
        /// Creates a new node from a string representation as created by <see cref="ToString"/>.
        /// </summary>
        /// <param name="line">The string representation to parse.</param>
        /// <returns>The newly created node.</returns>
        /// <exception cref="FormatException">The <paramref name="line"/> format is incorrect.</exception>
        internal static ManifestExecutableFile FromString(string line)
        {
            const int numberOfParts = 5;
            var parts = line.Split(new[] {' '}, numberOfParts);
            if (parts.Length != numberOfParts) throw new FormatException(Resources.InvalidNumberOfLineParts);

            try
            {
                return new ManifestExecutableFile(parts[1], FileUtils.FromUnixTime(long.Parse(parts[2])), long.Parse(parts[3]), parts[4]);
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
        /// <returns><c>"X", space, hash, space, mtime, space, size, space, file name, newline</c></returns>
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "X {0} {1} {2} {3}", Digest, ModifiedTimeUnix, Size, Name);
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(ManifestExecutableFile other) => base.Equals(other);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is ManifestExecutableFile file && Equals(file);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => base.GetHashCode();
        #endregion
    }
}
