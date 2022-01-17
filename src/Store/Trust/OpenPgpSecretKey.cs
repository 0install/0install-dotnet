// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Generator.Equals;

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Represents a secret key stored in a local <see cref="IOpenPgp"/> profile.
/// </summary>
/// <seealso cref="IOpenPgp.ListSecretKeys"/>
[PrimaryConstructor, Equatable]
public sealed partial class OpenPgpSecretKey : IFingerprintContainer
{
    /// <inheritdoc/>
    public long KeyID { get; }

    /// <inheritdoc/>
    [OrderedEquality]
    public byte[] Fingerprint { get; }

    /// <summary>
    /// The user's name, e-mail address, etc. of the key owner.
    /// </summary>
    public string UserID { get; }

    /// <summary>
    /// Returns the secret key in the form "UserID". Not safe for parsing!
    /// </summary>
    public override string ToString() => UserID;
}
