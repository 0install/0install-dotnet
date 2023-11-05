// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Trust;

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
    public static OpenPgpSecretKey GetSecretKey(this IOpenPgp openPgp, IKeyIDContainer keyIDContainer)
        => openPgp.ListSecretKeys()
                  .FirstOrDefault(x => x.KeyID == keyIDContainer.KeyID)
        ?? throw new KeyNotFoundException(Resources.UnableToFindSecretKey);

    /// <summary>
    /// Returns a specific secret key in the keyring.
    /// </summary>
    /// <param name="openPgp">The <see cref="IOpenPgp"/> implementation.</param>
    /// <param name="keySpecifier">The key ID, fingerprint or any part of a user ID that identifies the keypair; <c>null</c> to use the default key.</param>
    /// <exception cref="KeyNotFoundException">The specified key could not be found on the system.</exception>
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
            if (secretKeys.FirstOrDefault(x => x.KeyID == keyID) is {} key)
                return key;
        }
        catch (FormatException)
        {}

        try
        {
            var fingerprint = OpenPgpUtils.ParseFingerprint(keySpecifier);
            if (secretKeys.FirstOrDefault(x => x.Fingerprint.Equals(fingerprint)) is {} key)
                return key;
        }
        catch (FormatException)
        {}

        return secretKeys.FirstOrDefault(x => x.UserID.ContainsIgnoreCase(keySpecifier))
            ?? throw new KeyNotFoundException(Resources.UnableToFindSecretKey);
    }
}
