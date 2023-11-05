// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Represents a secret key stored in a local <see cref="IOpenPgp"/> profile.
/// </summary>
/// <param name="KeyID">The key ID of the key.</param>
/// <param name="Fingerprint">The fingerprint of the key.</param>
/// <param name="UserID">The user's name, e-mail address, etc. of the key owner.</param>
public sealed record OpenPgpSecretKey(long KeyID, OpenPgpFingerprint Fingerprint, string UserID) : IFingerprintContainer
{
    /// <summary>
    /// Returns the secret key in the form "UserID". Not safe for parsing!
    /// </summary>
    public override string ToString() => UserID;
}
