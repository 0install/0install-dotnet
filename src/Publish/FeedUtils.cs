// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;
using ZeroInstall.Store.Trust;
using StoreFeedUtils = ZeroInstall.Store.Feeds.FeedUtils;

namespace ZeroInstall.Publish;

/// <summary>
/// Helper methods for manipulating <see cref="Feed"/>s.
/// </summary>
public static class FeedUtils
{
    /// <summary>
    /// Writes an XSL stylesheet with its accompanying CSS file unless there is already an XSL in place.
    /// </summary>
    /// <param name="path">The directory to write the stylesheet files to.</param>
    /// <param name="name">The name of the stylesheet to deploy. Must be "feed" or "catalog".</param>
    /// <exception cref="IOException">Failed to write the stylesheet files.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the directory is not permitted.</exception>
    public static void DeployStylesheet(string path, [Localizable(false)] string name)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        #endregion

        if (!File.Exists(Path.Combine(path, $"{name}.xsl")))
        {
            DeployEmbeddedFile($"{name}.xsl", path);
            DeployEmbeddedFile($"{name}.css", path);
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "de":
                    DeployEmbeddedFile($"{name}.xsl.de", path);
                    break;
            }
        }
    }

    private static void DeployEmbeddedFile(string fileName, string targetDir)
        => typeof(FeedUtils).CopyEmbeddedToFile(fileName, Path.Combine(targetDir, fileName));

    /// <summary>
    /// Adds a Base64 signature to a feed or catalog stream.
    /// </summary>
    /// <param name="stream">The feed or catalog to sign.</param>
    /// <param name="secretKey">The secret key to use for signing the file.</param>
    /// <param name="passphrase">The passphrase to use to unlock the key.</param>
    /// <param name="openPgp">The OpenPGP-compatible system used to create signatures.</param>
    /// <exception cref="IOException">The file could not be read or written.</exception>
    /// <exception cref="UnauthorizedAccessException">Read or write access to the file is not permitted.</exception>
    /// <exception cref="KeyNotFoundException">The specified <paramref name="secretKey"/> could not be found on the system.</exception>
    /// <exception cref="WrongPassphraseException"><paramref name="passphrase"/> was incorrect.</exception>
    /// <remarks>
    /// The file is not parsed before signing; invalid XML files are signed as well.
    /// The existing file must end with a line break.
    /// Old signatures are not removed.
    /// </remarks>
    public static void SignFeed(Stream stream, OpenPgpSecretKey secretKey, string? passphrase, IOpenPgp openPgp)
    {
        #region Sanity checks
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (secretKey == null) throw new ArgumentNullException(nameof(secretKey));
        if (openPgp == null) throw new ArgumentNullException(nameof(openPgp));
        #endregion

        // Calculate the signature in-memory
        var signature = openPgp.Sign(stream.ReadAll(), secretKey, passphrase);

        // Add the signature to the end of the file
        var writer = new StreamWriter(stream, EncodingUtils.Utf8) {NewLine = "\n"};
        writer.Write(StoreFeedUtils.SignatureBlockStart);
        writer.WriteLine(Convert.ToBase64String(signature));
        writer.Write(StoreFeedUtils.SignatureBlockEnd);
        writer.Flush();
    }

    /// <summary>
    /// Determines the key used to sign a feed or catalog file. Only uses the first signature if more than one is present.
    /// </summary>
    /// <param name="path">The feed or catalog file to check for signatures.</param>
    /// <param name="openPgp">The OpenPGP-compatible system used to validate the signatures.</param>
    /// <returns>The key used to sign the file; <c>null</c> if the file was not signed.</returns>
    /// <exception cref="FileNotFoundException">The file file could not be found.</exception>
    /// <exception cref="IOException">The file could not be read.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
    public static OpenPgpSecretKey? GetKey(string path, IOpenPgp openPgp)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        if (openPgp == null) throw new ArgumentNullException(nameof(openPgp));
        #endregion

        try
        {
            var signature = StoreFeedUtils.GetSignatures(openPgp, File.ReadAllBytes(path))
                                          .OfType<ValidSignature>()
                                          .FirstOrDefault();
            if (signature == null) return null;

            return openPgp.GetSecretKey(signature);
        }
        #region Error handling
        catch (KeyNotFoundException)
        {
            Log.Info(Resources.SecretKeyNotInKeyring);
            return null;
        }
        catch (SignatureException ex)
        {
            Log.Error(ex);
            return null;
        }
        #endregion
    }
}
