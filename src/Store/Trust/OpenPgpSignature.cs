// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using Generator.Equals;

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// Represents a signature checked by an <see cref="IOpenPgp"/> implementation.
    /// </summary>
    /// <seealso cref="IOpenPgp.Verify"/>
    [PrimaryConstructor, Equatable]
    public abstract partial class OpenPgpSignature : IKeyIDContainer
    {
        /// <inheritdoc/>
        public long KeyID { get; }
    }

    /// <summary>
    /// Represents a valid signature.
    /// </summary>
    /// <seealso cref="IOpenPgp.Verify"/>
    [Equatable]
    public sealed partial class ValidSignature : OpenPgpSignature, IFingerprintContainer
    {
        /// <inheritdoc/>
        [OrderedEquality]
        public byte[] Fingerprint { get; }

        /// <summary>
        /// The point in time when the signature was created in UTC.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Creates a new valid signature.
        /// </summary>
        /// <param name="keyID">The key ID of the key used to create this signature.</param>
        /// <param name="fingerprint">The fingerprint of the key used to create this signature.</param>
        /// <param name="timestamp">The point in time when the signature was created in UTC.</param>
        public ValidSignature(long keyID, byte[] fingerprint, DateTime timestamp)
            : base(keyID)
        {
            Fingerprint = fingerprint ?? throw new ArgumentNullException(nameof(fingerprint));
            Timestamp = timestamp;
        }

        /// <summary>
        /// Returns the signature information in the form "ValidSignature: Fingerprint (Timestamp)". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"ValidSignature: {this.FormatFingerprint()} ({Timestamp})";
    }

    /// <summary>
    /// Represents a signature that could not be validated for some reason.
    /// </summary>
    /// <seealso cref="IOpenPgp.Verify"/>
    [PrimaryConstructor, Equatable]
    public partial class ErrorSignature : OpenPgpSignature
    {
        /// <summary>
        /// Returns the signature information in the form "ErrorSignature: KeyID". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"ErrorSignature: {this.FormatKeyID()}";
    }

    /// <summary>
    /// Represents a bad signature (i.e., the message has been tampered with).
    /// </summary>
    /// <seealso cref="IOpenPgp.Verify"/>
    [PrimaryConstructor, Equatable]
    public sealed partial class BadSignature : ErrorSignature
    {
        /// <summary>
        /// Returns the signature information in the form "BadSignature: KeyID". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"BadSignature: {this.FormatKeyID()}";
    }

    /// <summary>
    /// Represents a signature that could not yet be verified because the key is missing.
    /// Use <see cref="IOpenPgp.ImportKey"/> to import the key and then retry.
    /// </summary>
    /// <seealso cref="IOpenPgp.Verify"/>
    [PrimaryConstructor, Equatable]
    public sealed partial class MissingKeySignature : ErrorSignature
    {
        /// <summary>
        /// Returns the signature information in the form "MissingKeySignature: KeyID". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"MissingKeySignature: {this.FormatKeyID()}";
    }
}
