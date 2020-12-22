// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Globalization;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// A directory entry in a manifest.
    /// </summary>
    [Serializable]
    public sealed record ManifestDirectory(string FullPath)
        : ManifestNode
    {
        /// <summary>
        /// Creates a new node from a string representation as created by <see cref="ToString"/>.
        /// </summary>
        /// <param name="line">The string representation to parse.</param>
        /// <returns>The newly created node.</returns>
        /// <exception cref="FormatException">The <paramref name="line"/> format is incorrect.</exception>
        internal static ManifestDirectory FromString(string line)
        {
            const int numberOfParts = 2;
            string[] parts = line.Split(new[] {' '}, numberOfParts);
            if (parts.Length != numberOfParts) throw new FormatException(Resources.InvalidNumberOfLineParts);

            return new(parts[1]);
        }

        /// <summary>
        /// Returns the string representation of this node for the new manifest format.
        /// </summary>
        /// <returns><c>"D", space, full path name, newline</c></returns>
        public override string ToString()
            => string.Format(CultureInfo.InvariantCulture, "D {0}", FullPath);
    }
}
