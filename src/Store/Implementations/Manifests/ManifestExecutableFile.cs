// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Globalization;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// An executable file entry in a manifest.
    /// </summary>
    [Serializable]
    public sealed record ManifestExecutableFile(string Digest, long ModifiedTimeUnix, long Size, string Name)
        : ManifestFileBase(Digest, ModifiedTimeUnix, Size, Name)
    {
        /// <summary>
        /// Creates a new executable file entry.
        /// </summary>
        /// <param name="digest">The digest of the content of the file calculated using the selected digest algorithm.</param>
        /// <param name="modifiedTime">The time this file was last modified.</param>
        /// <param name="size">The size of the file in bytes.</param>
        /// <param name="name">The name of the file without the containing directory.</param>
        public ManifestExecutableFile(string digest, DateTime modifiedTime, long size, string name)
            : this(digest, modifiedTime.ToUnixTime(), size, name)
        {}

        /// <summary>
        /// Creates a new node from a string representation as created by <see cref="ToString"/>.
        /// </summary>
        /// <param name="line">The string representation to parse.</param>
        /// <returns>The newly created node.</returns>
        /// <exception cref="FormatException">The <paramref name="line"/> format is incorrect.</exception>
        internal static ManifestExecutableFile FromString(string line)
        {
            const int numberOfParts = 5;
            string[] parts = line.Split(new[] {' '}, numberOfParts);
            if (parts.Length != numberOfParts) throw new FormatException(Resources.InvalidNumberOfLineParts);

            try
            {
                return new(parts[1], long.Parse(parts[2]), long.Parse(parts[3]), parts[4]);
            }
            #region Error handling
            catch (OverflowException ex)
            {
                throw new FormatException(Resources.NumberTooLarge, ex);
            }
            #endregion
        }

        /// <summary>
        /// Returns the string representation of this node for the manifest format.
        /// </summary>
        /// <returns><c>"X", space, hash, space, mtime, space, size, space, file name, newline</c></returns>
        public override string ToString()
            => string.Format(CultureInfo.InvariantCulture, "X {0} {1} {2} {3}", Digest, ModifiedTimeUnix, Size, Name);
    }
}
