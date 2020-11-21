// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Linq;
using NanoByte.Common.Collections;

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// Represents a secret key stored in a local <see cref="IOpenPgp"/> profile.
    /// </summary>
    /// <seealso cref="IOpenPgp.ListSecretKeys"/>
    public sealed class OpenPgpSecretKey : IFingerprintContainer, IEquatable<OpenPgpSecretKey>
    {
        /// <inheritdoc/>
        public long KeyID { get; }

        private readonly byte[] _fingerprint;

        /// <inheritdoc/>
        public byte[] GetFingerprint() => _fingerprint.ToArray();

        /// <summary>
        /// The user's name, e-mail address, etc. of the key owner.
        /// </summary>
        public string UserID { get; }

        /// <summary>
        /// Creates a new <see cref="IOpenPgp"/> secret key representation.
        /// </summary>
        /// <param name="keyID">A short identifier for the key.</param>
        /// <param name="fingerprint">A long identifier for the key.</param>
        /// <param name="userID">The user's name, e-mail address, etc. of the key owner.</param>
        public OpenPgpSecretKey(long keyID, byte[] fingerprint, string userID)
        {
            KeyID = keyID;
            _fingerprint = fingerprint ?? throw new ArgumentNullException(nameof(fingerprint));
            UserID = userID ?? throw new ArgumentNullException(nameof(userID));
        }

        #region Conversion
        /// <summary>
        /// Returns the secret key in the form "UserID". Not safe for parsing!
        /// </summary>
        public override string ToString() => UserID;
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(OpenPgpSecretKey? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return KeyID == other.KeyID
                && _fingerprint.SequencedEquals(other._fingerprint)
                && UserID == other.UserID;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is OpenPgpSecretKey key && Equals(key);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                KeyID,
                _fingerprint.GetSequencedHashCode(),
                UserID);

        public static bool operator ==(OpenPgpSecretKey? left, OpenPgpSecretKey? right) => Equals(left, right);
        public static bool operator !=(OpenPgpSecretKey? left, OpenPgpSecretKey? right) => !Equals(left, right);
        #endregion
    }
}
