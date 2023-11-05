// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Represents a signature checked by an <see cref="IOpenPgp"/> implementation.
/// </summary>
public abstract record OpenPgpSignature(long KeyID) : IKeyIDContainer;

/// <summary>
/// Represents a valid signature.
/// </summary>
/// <param name="KeyID">The key ID of the key used to create this signature.</param>
/// <param name="Fingerprint">The fingerprint of the key used to create this signature.</param>
/// <param name="Timestamp">The point in time when the signature was created in UTC.</param>
public sealed record ValidSignature(long KeyID, OpenPgpFingerprint Fingerprint, DateTime Timestamp) : OpenPgpSignature(KeyID), IFingerprintContainer
{
    /// <summary>
    /// Returns the signature information in the form "ValidSignature: Fingerprint (Timestamp)". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"ValidSignature: {this.FormatFingerprint()} ({Timestamp})";
}

/// <summary>
/// Represents a signature that could not be validated for some reason.
/// </summary>
/// <param name="KeyID">The key ID of the key used to create this signature.</param>
public record ErrorSignature(long KeyID) : OpenPgpSignature(KeyID)
{
    /// <summary>
    /// Returns the signature information in the form "ErrorSignature: KeyID". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"ErrorSignature: {this.FormatKeyID()}";
}

/// <summary>
/// Represents a bad signature (i.e., the message has been tampered with).
/// </summary>
/// &lt;param name="KeyID"&gt;The key ID of the key used to create this signature.&lt;/param&gt;
public sealed record BadSignature(long KeyID) : ErrorSignature(KeyID)
{
    /// <summary>
    /// Returns the signature information in the form "BadSignature: KeyID". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"BadSignature: {this.FormatKeyID()}";
}

/// <summary>
/// Represents a signature that could not yet be verified because the key is missing.
/// </summary>
/// <param name="KeyID">The key ID of the key used to create this signature.</param>
public sealed record MissingKeySignature(long KeyID) : ErrorSignature(KeyID)
{
    /// <summary>
    /// Returns the signature information in the form "MissingKeySignature: KeyID". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"MissingKeySignature: {this.FormatKeyID()}";
}
