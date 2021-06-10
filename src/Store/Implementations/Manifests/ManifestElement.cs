// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// Base class for directory-element entries (files and symlinks) in a <see cref="Manifest"/>.
    /// </summary>
    /// <param name="Digest">The digest of the content of the element calculated using the selected digest algorithm.</param>
    /// <param name="Size">The size of the element in bytes.</param>
    [Serializable]
    public abstract record ManifestElement(string Digest, long Size);

    /// <summary>
    /// A symlink entry in a <see cref="Manifest"/>.
    /// </summary>
    /// <param name="Digest">The digest of the link target path.</param>
    /// <param name="Size">The length of the link target path.</param>
    [Serializable]
    public sealed record ManifestSymlink(string Digest, long Size)
        : ManifestElement(Digest, Size);

    /// <summary>
    /// Base class for file entries in a <see cref="Manifest"/>.
    /// </summary>
    /// <param name="Digest">The digest of the content of the file calculated using the selected digest algorithm.</param>
    /// <param name="ModifiedTime">The time this file was last modified as Unix time.</param>
    /// <param name="Size">The size of the file in bytes.</param>
    [Serializable]
    public abstract record ManifestFile(string Digest, long ModifiedTime, long Size)
        : ManifestElement(Digest, Size);

    /// <summary>
    /// An non-executable file entry in a <see cref="Manifest"/>.
    /// </summary>
    /// <param name="Digest">The digest of the content of the file calculated using the selected digest algorithm.</param>
    /// <param name="ModifiedTime">The time this file was last modified as Unix time.</param>
    /// <param name="Size">The size of the file in bytes.</param>
    [Serializable]
    public sealed record ManifestNormalFile(string Digest, long ModifiedTime, long Size)
        : ManifestFile(Digest, ModifiedTime, Size);

    /// <summary>
    /// An executable file entry in a <see cref="Manifest"/>.
    /// </summary>
    /// <param name="Digest">The digest of the content of the file calculated using the selected digest algorithm.</param>
    /// <param name="ModifiedTime">The time this file was last modified as Unix time.</param>
    /// <param name="Size">The size of the file in bytes.</param>
    [Serializable]
    public sealed record ManifestExecutableFile(string Digest, long ModifiedTime, long Size)
        : ManifestFile(Digest, ModifiedTime, Size);
}
