// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// An object containing a key ID for an <see cref="IOpenPgp"/> public or private key.
    /// </summary>
    public interface IKeyIDContainer
    {
        /// <summary>
        /// An OpenPGP key ID. A short identifier for a key. The lower 64 bits of <see cref="IFingerprintContainer.GetFingerprint"/>.
        /// </summary>
        long KeyID { get; }
    }
}
