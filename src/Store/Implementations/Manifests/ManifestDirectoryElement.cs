// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// An abstract base class for directory-element entries (files and symlinks) in a manifest.
    /// </summary>
    [Serializable]
    public abstract record ManifestDirectoryElement(string Digest, long Size, string Name)
        : ManifestNode;
}
