// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Helper methods for <see cref="IKeyIDContainer"/> and <see cref="IFingerprintContainer"/>.
/// </summary>
public static class OpenPgpUtils
{
    /// <summary>
    /// Formats a key ID as a canonical string.
    /// </summary>
    public static string FormatKeyID(this IKeyIDContainer keyIDContainer)
    {
        #region Sanity checks
        if (keyIDContainer == null) throw new ArgumentNullException(nameof(keyIDContainer));
        #endregion

        return keyIDContainer.KeyID.ToString("x16").ToUpperInvariant();
    }

    /// <summary>
    /// Formats a key fingerprint as a canonical string.
    /// </summary>
    public static string FormatFingerprint(this IFingerprintContainer fingerprintContainer)
    {
        #region Sanity checks
        if (fingerprintContainer == null) throw new ArgumentNullException(nameof(fingerprintContainer));
        #endregion

        return BitConverter.ToString(fingerprintContainer.Fingerprint).Replace("-", "");
    }

    /// <summary>
    /// Parses a canonical string formatting of a key ID.
    /// </summary>
    /// <exception cref="FormatException">The string format is not valid.</exception>
    internal static long ParseKeyID(string keyID)
    {
        #region Sanity checks
        if (keyID == null) throw new ArgumentNullException(nameof(keyID));
        #endregion

        if (keyID.Length != 16) throw new FormatException("OpenPGP key ID string representation must be 16 characters long.");

        return Convert.ToInt64(keyID, fromBase: 16);
    }

    /// <summary>
    /// Parses a canonical string formatting of a key fingerprint.
    /// </summary>
    /// <exception cref="FormatException">The string format is not valid.</exception>
    internal static byte[] ParseFingerprint(string fingerprint)
    {
        #region Sanity checks
        if (fingerprint == null) throw new ArgumentNullException(nameof(fingerprint));
        #endregion

        var result = new byte[fingerprint.Length / 2];
        for (int i = 0; i < result.Length; i++)
            result[i] = Convert.ToByte(fingerprint.Substring(i * 2, 2), 16);
        return result;
    }

    /// <summary>
    /// Extracts the key ID from a key fingerprint.
    /// </summary>
    internal static long FingerprintToKeyID(byte[] fingerprint)
    {
        #region Sanity checks
        if (fingerprint == null) throw new ArgumentNullException(nameof(fingerprint));
        #endregion

        // Extract lower 64 bits and treat as Big Endian
        unchecked
        {
            int i1 = (fingerprint[fingerprint.Length - 8] << 24) | (fingerprint[fingerprint.Length - 7] << 16) | (fingerprint[fingerprint.Length - 6] << 8) | fingerprint[fingerprint.Length - 5];
            int i2 = (fingerprint[fingerprint.Length - 4] << 24) | (fingerprint[fingerprint.Length - 3] << 16) | (fingerprint[fingerprint.Length - 2] << 8) | fingerprint[fingerprint.Length - 1];
            return ((long)i1 << 32) | (uint)i2;
        }
    }

    /// <summary>
    /// Exports an OpenPGP public key to a key file.
    /// </summary>
    /// <param name="openPgp">The OpenPGP-compatible system used to manage keys.</param>
    /// <param name="keyID">The key ID to get the public key for.</param>
    /// <param name="path">The directory to write the key file to.</param>
    /// <exception cref="UnauthorizedAccessException">The file could not be read or written.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the directory is not permitted.</exception>
    /// <exception cref="IOException">The specified <paramref name="keyID"/> could not be found on the system.</exception>
    public static void DeployPublicKey(this IOpenPgp openPgp, IKeyIDContainer keyID, string path)
    {
        #region Sanity checks
        if (openPgp == null) throw new ArgumentNullException(nameof(openPgp));
        if (keyID == null) throw new ArgumentNullException(nameof(keyID));
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        #endregion

        File.WriteAllText(
            path: Path.Combine(path, $"{keyID.FormatKeyID()}.gpg"),
            contents: openPgp.ExportKey(keyID),
            encoding: Encoding.ASCII);
    }
}
