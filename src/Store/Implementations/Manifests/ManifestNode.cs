// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;

namespace ZeroInstall.Store.Implementations.Manifests
{
    /// <summary>
    /// An abstract base class for entries in manifest.
    /// </summary>
    [Serializable]
    public abstract record ManifestNode;
}
