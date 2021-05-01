// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics.CodeAnalysis;

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// An object containing a fingerprint for an <see cref="IOpenPgp"/> public or private key.
    /// </summary>
    public interface IFingerprintContainer : IKeyIDContainer
    {
        /// <summary>
        /// Returns an OpenPGP key fingerprint. A long identifier for a key. Superset of <see cref="IKeyIDContainer.KeyID"/>.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        byte[] GetFingerprint();
    }
}
