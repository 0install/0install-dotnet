// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Globalization;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// A symlink entry in a manifest.
    /// </summary>
    [Serializable]
    public sealed record ManifestSymlink(string Digest, long Size, string Name)
        : ManifestDirectoryElement(Digest, Size, Name)
    {
        /// <summary>
        /// Creates a new node from a string representation as created by <see cref="ToString"/>.
        /// </summary>
        /// <param name="line">The string representation to parse.</param>
        /// <returns>The newly created node.</returns>
        /// <exception cref="FormatException">The <paramref name="line"/> format is incorrect.</exception>
        internal static ManifestSymlink FromString(string line)
        {
            const int numberOfParts = 4;
            string[] parts = line.Split(new[] {' '}, numberOfParts);
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

        /// <summary>
        /// Returns the string representation of this node for the manifest format.
        /// </summary>
        /// <returns><c>"S", space, hash, space, size, space, symlink name, newline</c></returns>
        public override string ToString()
            => string.Format(CultureInfo.InvariantCulture, "S {0} {1} {2}", Digest, Size, Name);
    }
}
