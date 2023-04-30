// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Provides access to an encryption/signature system compatible with the OpenPGP standard.
/// </summary>
public interface IOpenPgp
{
    /// <summary>
    /// Verifies a detached OpenPGP signature.
    /// </summary>
    /// <param name="data">The data the signature is for.</param>
    /// <param name="signature">The signature in binary format.</param>
    /// <returns>A list of signatures found, both valid and invalid. <see cref="MissingKeySignature"/> results indicate you need to use <see cref="ImportKey"/>.</returns>
    /// <exception cref="SignatureException"><paramref name="signature"/> does not contain syntactically correct signature data.</exception>
    IEnumerable<OpenPgpSignature> Verify(ArraySegment<byte> data, byte[] signature);

    /// <summary>
    /// Creates a detached OpenPGP signature using a specific secret key.
    /// </summary>
    /// <param name="data">The data to sign.</param>
    /// <param name="secretKey">The secret key to use for signing.</param>
    /// <param name="passphrase">The passphrase to use to unlock the secret key.</param>
    /// <returns>The signature in binary format.</returns>
    /// <exception cref="KeyNotFoundException">The specified <paramref name="secretKey"/> could not be found in the keyring.</exception>
    /// <exception cref="WrongPassphraseException"><paramref name="passphrase"/> was incorrect.</exception>
    byte[] Sign(ArraySegment<byte> data, OpenPgpSecretKey secretKey, string? passphrase = null);

    /// <summary>
    /// Imports a public key into the keyring.
    /// </summary>
    /// <param name="data">The public key in binary or ASCII Armored format.</param>
    /// <exception cref="InvalidDataException"><paramref name="data"/> does not contain a valid public key.</exception>
    void ImportKey(ArraySegment<byte> data);

    /// <summary>
    /// Exports the public key for a specific key in the keyring.
    /// </summary>
    /// <param name="keyIDContainer">An object containing the key ID of the public key to export.</param>
    /// <returns>The public key in ASCII Armored format. Always uses Unix-style linebreaks.</returns>
    /// <exception cref="KeyNotFoundException">The specified <paramref name="keyIDContainer"/> could not be found in the keyring.</exception>
    string ExportKey(IKeyIDContainer keyIDContainer);

    /// <summary>
    /// Returns a list of secret keys in the keyring.
    /// </summary>
    IEnumerable<OpenPgpSecretKey> ListSecretKeys();
}
