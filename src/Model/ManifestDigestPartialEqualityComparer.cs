// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Uses <see cref="ManifestDigest.PartialEquals"/> to compare <see cref="ManifestDigest"/>s.
    /// </summary>
    public sealed class ManifestDigestPartialEqualityComparer : IEqualityComparer<ManifestDigest>
    {
        /// <summary>A singleton instance of the comparer.</summary>
        public static readonly ManifestDigestPartialEqualityComparer Instance = new ManifestDigestPartialEqualityComparer();

        private ManifestDigestPartialEqualityComparer() {}

        /// <inheritdoc/>
        public bool Equals(ManifestDigest x, ManifestDigest y) => x.PartialEquals(y);

        /// <summary>
        /// Always returns 0. The concept of hashing is not applicable to partial equality.
        /// </summary>
        public int GetHashCode(ManifestDigest obj) => 0;
    }

    /// <summary>
    /// Uses <see cref="ManifestDigest.PartialEquals"/> to compare <see cref="ImplementationBase"/>s.
    /// </summary>
    public sealed class ManifestDigestPartialEqualityComparer<T> : IEqualityComparer<T> where T : ImplementationBase
    {
        /// <summary>A singleton instance of the comparer.</summary>
        public static readonly ManifestDigestPartialEqualityComparer<T> Instance = new ManifestDigestPartialEqualityComparer<T>();

        private ManifestDigestPartialEqualityComparer() {}

        /// <inheritdoc/>
        public bool Equals(T x, T y)
        {
            if (x == null || y == null) return false;
            return x.ManifestDigest.PartialEquals(y.ManifestDigest);
        }

        /// <summary>
        /// Always returns 0. The concept of hashing is not applicable to partial equality.
        /// </summary>
        public int GetHashCode(T obj) => 0;
    }
}
