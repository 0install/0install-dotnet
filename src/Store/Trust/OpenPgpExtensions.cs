// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Linq;
using NanoByte.Common;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// Provides extension methods for <see cref="IOpenPgp"/> implementations.
    /// </summary>
    public static class OpenPgpExtensions
    {
        /// <summary>
        /// Returns a specific secret key in the keyring.
        /// </summary>
        /// <param name="openPgp">The <see cref="IOpenPgp"/> implementation.</param>
        /// <param name="keyIDContainer">An object containing the key ID that identifies the keypair.</param>
        /// <exception cref="KeyNotFoundException">The specified key could not be found on the system.</exception>
        /// <seealso cref="IOpenPgp.Sign"/>
        /// <seealso cref="IOpenPgp.ExportKey"/>
        public static OpenPgpSecretKey GetSecretKey(this IOpenPgp openPgp, IKeyIDContainer keyIDContainer)
        {
            #region Sanity checks
            if (openPgp == null) throw new ArgumentNullException(nameof(openPgp));
            if (keyIDContainer == null) throw new ArgumentNullException(nameof(keyIDContainer));
            #endregion

            var secretKeys = openPgp.ListSecretKeys().ToList();
            if (secretKeys.Count == 0)
                throw new KeyNotFoundException(Resources.UnableToFindSecretKey);

            try
            {
                return secretKeys.First(x => x.KeyID == keyIDContainer.KeyID);
            }
            catch (InvalidOperationException)
            {
                throw new KeyNotFoundException(Resources.UnableToFindSecretKey);
            }
        }

        /// <summary>
        /// Returns a specific secret key in the keyring.
        /// </summary>
        /// <param name="openPgp">The <see cref="IOpenPgp"/> implementation.</param>
        /// <param name="keySpecifier">The key ID, fingerprint or any part of a user ID that identifies the keypair; <c>null</c> to use the default key.</param>
        /// <exception cref="KeyNotFoundException">The specified key could not be found on the system.</exception>
        /// <seealso cref="IOpenPgp.Sign"/>
        /// <seealso cref="IOpenPgp.ExportKey"/>
        public static OpenPgpSecretKey GetSecretKey(this IOpenPgp openPgp, string? keySpecifier = null)
        {
            #region Sanity checks
            if (openPgp == null) throw new ArgumentNullException(nameof(openPgp));
            #endregion

            var secretKeys = openPgp.ListSecretKeys().ToList();
            if (secretKeys.Count == 0)
                throw new KeyNotFoundException(Resources.UnableToFindSecretKey);

            if (string.IsNullOrEmpty(keySpecifier))
                return secretKeys[0];

            try
            {
                long keyID = OpenPgpUtils.ParseKeyID(keySpecifier);
                return secretKeys.First(x => x.KeyID == keyID);
            }
            catch (FormatException)
            {}
            catch (InvalidOperationException)
            {}

            try
            {
                var fingerprint = OpenPgpUtils.ParseFingerprint(keySpecifier);
                return secretKeys.First(x => x.GetFingerprint().SequenceEqual(fingerprint));
            }
            catch (FormatException)
            {}
            catch (InvalidOperationException)
            {}

            try
            {
                return secretKeys.First(x => x.UserID.ContainsIgnoreCase(keySpecifier));
            }
            catch
            {
                throw new KeyNotFoundException(Resources.UnableToFindSecretKey);
            }
        }
    }
}
