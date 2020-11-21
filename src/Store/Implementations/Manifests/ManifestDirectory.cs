// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Globalization;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// A directory entry in a <see cref="Manifest"/>.
    /// </summary>
    /// <remarks>This class is immutable. It should only be used as a part of a <see cref="Manifest"/>.</remarks>
    [Serializable]
    public sealed class ManifestDirectory : ManifestNode, IEquatable<ManifestDirectory>
    {
        /// <summary>
        /// The complete path of this directory relative to the tree root as a Unix-Path beginning with a slash.
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// Creates a new directory-entry.
        /// </summary>
        /// <param name="fullPath">The complete path of this directory relative to the tree root as a Unix-Path beginning with a slash.</param>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> contains a newline character.</exception>
        public ManifestDirectory(string fullPath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(fullPath)) throw new ArgumentNullException(nameof(fullPath));
            if (fullPath.Contains("\n")) throw new ArgumentException(Resources.NewlineInName, nameof(fullPath));
            #endregion

            FullPath = fullPath;
        }

        #region Factory methods
        /// <summary>
        /// Creates a new node from a string representation as created by <see cref="ToString"/>.
        /// </summary>
        /// <param name="line">The string representation to parse.</param>
        /// <returns>The newly created node.</returns>
        /// <exception cref="FormatException">The <paramref name="line"/> format is incorrect.</exception>
        internal static ManifestDirectory FromString(string line)
        {
            const int numberOfParts = 2;
            var parts = line.Split(new[] {' '}, numberOfParts);
            if (parts.Length != numberOfParts) throw new FormatException(Resources.InvalidNumberOfLineParts);

            return new ManifestDirectory(parts[1]);
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the string representation of this node for the new manifest format.
        /// </summary>
        /// <returns><c>"D", space, full path name, newline</c></returns>
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "D {0}", FullPath);
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(ManifestDirectory? other)
            => other != null && FullPath == other.FullPath;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is ManifestDirectory directory && Equals(directory);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => FullPath.GetHashCode();
        #endregion
    }
}
