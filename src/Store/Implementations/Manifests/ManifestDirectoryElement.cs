// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// An abstract base class for directory-element entries (files and symlinks) in a manifest.
    /// </summary>
    /// <param name="Digest">The digest of the content of the element calculated using the selected digest algorithm.</param>
    /// <param name="Size">The size of the element in bytes.</param>
    /// <param name="Name">The name of the element without the containing directory.</param>
    [Serializable]
    public abstract record ManifestDirectoryElement(string Digest, long Size, string Name)
        : ManifestNode, IComparable<ManifestDirectoryElement>
    {
        /// <inheritdoc/>
        public int CompareTo(ManifestDirectoryElement? other)
        {
            if (other == this) return 0;
            else if (other == null) return 1;
            else return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }
    }
}
