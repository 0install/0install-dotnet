// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common.Storage;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// An abstract base class for file entries in a manifest.
    /// </summary>
    /// <param name="Digest">The digest of the content of the file calculated using the selected digest algorithm.</param>
    /// <param name="ModifiedTimeUnix">The time this file was last modified in unix time.</param>
    /// <param name="Size">The size of the file in bytes.</param>
    /// <param name="Name">The name of the file without the containing directory.</param>
    [Serializable]
    public abstract record ManifestFileBase(string Digest, long ModifiedTimeUnix, long Size, string Name)
        : ManifestDirectoryElement(Digest, Size, Name)
    {
        /// <summary>
        /// The time this file was last modified.
        /// </summary>
        public DateTime ModifiedTime => FileUtils.FromUnixTime(ModifiedTimeUnix);
    }
}
