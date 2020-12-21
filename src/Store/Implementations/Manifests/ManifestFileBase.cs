// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common.Storage;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// An abstract base class for file entries in a manifest.
    /// </summary>
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
