// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Linq;
using NanoByte.Common.Collections;

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// Represents a signature checked by an <see cref="IOpenPgp"/> implementation.
    /// </summary>
    /// <seealso cref="IOpenPgp.Verify"/>
    public abstract class OpenPgpSignature : IKeyIDContainer
    {
        /// <inheritdoc/>
        public long KeyID { get; }

        /// <summary>
        /// Creates a new signature.
        /// </summary>
        /// <param name="keyID">The key ID of the key used to create this signature.</param>
        protected OpenPgpSignature(long keyID) { KeyID = keyID; }

        #region Equality
        protected bool Equals(OpenPgpSignature other)
            => KeyID == other.KeyID;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((OpenPgpSignature)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => KeyID.GetHashCode();
        #endregion
    }

    /// <summary>
    /// Represents a valid signature.
    /// </summary>
    /// <seealso cref="IOpenPgp.Verify"/>
    public sealed class ValidSignature : OpenPgpSignature, IFingerprintContainer
    {
        private readonly byte[] _fingerprint;

        /// <inheritdoc/>
        public byte[] GetFingerprint() => _fingerprint.ToArray();

        /// <summary>
        /// The point in time when the signature was created in UTC.
        /// </summary>
        public readonly DateTime Timestamp;

        /// <summary>
        /// Creates a new valid signature.
        /// </summary>
        /// <param name="keyID">The key ID of the key used to create this signature.</param>
        /// <param name="fingerprint">The fingerprint of the key used to create this signature.</param>
        /// <param name="timestamp">The point in time when the signature was created in UTC.</param>
        public ValidSignature(long keyID, byte[] fingerprint, DateTime timestamp)
            : base(keyID)
        {
            _fingerprint = fingerprint ?? throw new ArgumentNullException(nameof(fingerprint));
            Timestamp = timestamp;
        }

        /// <summary>
        /// Returns the signature information in the form "ValidSignature: Fingerprint (Timestamp)". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"ValidSignature: {this.FormatFingerprint()} ({Timestamp})";

        #region Equality
        private bool Equals(ValidSignature other)
            => base.Equals(other)
            && _fingerprint.SequencedEquals(other._fingerprint)
            && Timestamp == other.Timestamp;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is ValidSignature signature && Equals(signature);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(),
                _fingerprint.GetSequencedHashCode(),
                Timestamp);
        #endregion
    }

    /// <summary>
    /// Represents a signature that could not be validated for some reason.
    /// </summary>
    /// <seealso cref="IOpenPgp.Verify"/>
    public class ErrorSignature : OpenPgpSignature
    {
        /// <summary>
        /// Creates a new signature error.
        /// </summary>
        /// <param name="keyID">The key ID of the key used to create this signature.</param>
        public ErrorSignature(long keyID)
            : base(keyID)
        {}

        /// <summary>
        /// Returns the signature information in the form "ErrorSignature: KeyID". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"ErrorSignature: {this.FormatKeyID()}";

        #region Equality
        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is ErrorSignature signature && Equals(signature);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }

    /// <summary>
    /// Represents a bad signature (i.e., the message has been tampered with).
    /// </summary>
    /// <seealso cref="IOpenPgp.Verify"/>
    public sealed class BadSignature : ErrorSignature
    {
        /// <summary>
        /// Creates a new bad signature.
        /// </summary>
        /// <param name="keyID">The key ID of the key used to create this signature.</param>
        public BadSignature(long keyID)
            : base(keyID)
        {}

        /// <summary>
        /// Returns the signature information in the form "BadSignature: KeyID". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"BadSignature: {this.FormatKeyID()}";

        #region Equality
        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is BadSignature signature && Equals(signature);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }

    /// <summary>
    /// Represents a signature that could not yet be verified because the key is missing.
    /// Use <see cref="IOpenPgp.ImportKey"/> to import the key and then retry.
    /// </summary>
    /// <seealso cref="IOpenPgp.Verify"/>
    public sealed class MissingKeySignature : ErrorSignature
    {
        /// <summary>
        /// Creates a new missing key error.
        /// </summary>
        /// <param name="keyID">The key ID of the key used to create this signature.</param>
        public MissingKeySignature(long keyID)
            : base(keyID)
        {}

        /// <summary>
        /// Returns the signature information in the form "MissingKeySignature: KeyID". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"MissingKeySignature: {this.FormatKeyID()}";

        #region Equality
        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is MissingKeySignature signature && Equals(signature);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
}
