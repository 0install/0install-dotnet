// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common.Collections;

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// Represents a secret key stored in a local <see cref="IOpenPgp"/> profile.
    /// </summary>
    /// <seealso cref="IOpenPgp.ListSecretKeys"/>
    [PrimaryConstructor]
    public sealed partial class OpenPgpSecretKey : IFingerprintContainer, IEquatable<OpenPgpSecretKey>
    {
        /// <inheritdoc/>
        public long KeyID { get; }

        /// <inheritdoc/>
        public byte[] Fingerprint { get; }

        /// <summary>
        /// The user's name, e-mail address, etc. of the key owner.
        /// </summary>
        public string UserID { get; }

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
                && Fingerprint.SequencedEquals(other.Fingerprint)
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
                Fingerprint.GetSequencedHashCode(),
                UserID);

        public static bool operator ==(OpenPgpSecretKey? left, OpenPgpSecretKey? right) => Equals(left, right);
        public static bool operator !=(OpenPgpSecretKey? left, OpenPgpSecretKey? right) => !Equals(left, right);
        #endregion
    }
}
