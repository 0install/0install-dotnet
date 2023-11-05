// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Trust;

/// <summary>
/// An object containing a fingerprint for an <see cref="IOpenPgp"/> public or private key.
/// </summary>
public interface IFingerprintContainer : IKeyIDContainer
{
    /// <summary>
    /// An OpenPGP key fingerprint. Superset of <see cref="IKeyIDContainer.KeyID"/>.
    /// </summary>
    OpenPgpFingerprint Fingerprint { get; }
}
