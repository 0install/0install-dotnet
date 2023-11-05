// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Provides access to the signature functions of GnuPG.
/// </summary>
public partial class GnuPG : IOpenPgp
{
    private readonly GpgLauncher _gpg;

    /// <summary>
    /// Creates a new GnuPG instance.
    /// </summary>
    /// <param name="homeDir">The GnuPG home dir to use.</param>
    public GnuPG(string homeDir)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(homeDir)) throw new ArgumentNullException(nameof(homeDir));
        #endregion

        _gpg = new(homeDir);
    }

    /// <inheritdoc/>
    public IEnumerable<OpenPgpSignature> Verify(ArraySegment<byte> data, byte[] signature)
    {
        #region Sanity checks
        if (signature == null) throw new ArgumentNullException(nameof(signature));
        #endregion

        string result;
        using (var signatureFile = new TemporaryFile("0install-sig"))
        {
            File.WriteAllBytes(signatureFile, signature);
            result = _gpg.RunAndCapture(data, "--status-fd", "1", "--verify", signatureFile.Path, "-");
        }
        var lines = result.SplitMultilineText();

        // Each signature is represented by one line of encoded information
        var signatures = new List<OpenPgpSignature>(lines.Length);
        foreach (string line in lines)
        {
            try
            {
                if (ParseSignatureLine(line) is {} parsedSignature)
                    signatures.Add(parsedSignature);
            }
            #region Error handling
            catch (FormatException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion
        }

        return signatures;
    }

    /// <summary>
    /// Parses information about a signature from a console line.
    /// </summary>
    /// <param name="line">The console line containing the signature information.</param>
    /// <returns>The parsed signature representation; <c>null</c> if <paramref name="line"/> did not contain any signature information.</returns>
    /// <exception cref="FormatException"><paramref name="line"/> contains incorrectly formatted signature information.</exception>
    private static OpenPgpSignature? ParseSignatureLine(string line)
    {
        const int signatureTypeIndex = 1, fingerprintIndex = 2, timestampIndex = 4, keyIDIndex = 2, errorCodeIndex = 7;

        var signatureParts = line.Split(' ');
        if (signatureParts.Length < signatureTypeIndex + 1) return null;
        switch (signatureParts[signatureTypeIndex])
        {
            case "VALIDSIG":
                if (signatureParts.Length != 12) throw new FormatException("Incorrect number of columns in VALIDSIG line.");
                var fingerprint = OpenPgpUtils.ParseFingerprint(signatureParts[fingerprintIndex]);
                return new ValidSignature(
                    OpenPgpUtils.FingerprintToKeyID(fingerprint),
                    fingerprint,
                    Timestamp: new UnixTime(long.Parse(signatureParts[timestampIndex])));

            case "BADSIG":
                if (signatureParts.Length < 3) throw new FormatException("Incorrect number of columns in BADSIG line.");
                return new BadSignature(OpenPgpUtils.ParseKeyID(signatureParts[keyIDIndex]));

            case "ERRSIG":
                if (signatureParts.Length != 8) throw new FormatException("Incorrect number of columns in ERRSIG line.");
                return int.Parse(signatureParts[errorCodeIndex]) switch
                {
                    9 => new MissingKeySignature(OpenPgpUtils.ParseKeyID(signatureParts[keyIDIndex])),
                    _ => new ErrorSignature(OpenPgpUtils.ParseKeyID(signatureParts[keyIDIndex]))
                };

            default:
                return null;
        }
    }

    /// <inheritdoc/>
    public byte[] Sign(ArraySegment<byte> data, OpenPgpSecretKey secretKey, string? passphrase = null)
    {
        #region Sanity checks
        if (secretKey == null) throw new ArgumentNullException(nameof(secretKey));
        #endregion

        return Convert.FromBase64String(
            _gpg
               .RunAndCapture(data, "--passphrase", passphrase ?? "", "--local-user", secretKey.FormatKeyID(), "--detach-sign", "--armor", "--output", "-", "-")
               .GetRightPartAtFirstOccurrence(Environment.NewLine + Environment.NewLine)
               .GetLeftPartAtLastOccurrence(Environment.NewLine + "=")
               .Replace(Environment.NewLine, "\n"));
    }

    /// <inheritdoc/>
    public void ImportKey(ArraySegment<byte> data)
        => _gpg.RunAndCapture(data, "--quiet", "--import");

    /// <inheritdoc/>
    public string ExportKey(IKeyIDContainer keyIDContainer)
    {
        #region Sanity checks
        if (keyIDContainer == null) throw new ArgumentNullException(nameof(keyIDContainer));
        #endregion

        string result = _gpg.RunAndCapture("--armor", "--export", keyIDContainer.FormatKeyID());
        return result.Replace(Environment.NewLine, "\n") + "\n";
    }

    /// <inheritdoc/>
    public IEnumerable<OpenPgpSecretKey> ListSecretKeys()
    {
        string result = _gpg.RunAndCapture("--list-secret-keys", "--with-colons", "--fixed-list-mode", "--fingerprint");

        string[]? sec = null, fpr = null, uid = null;
        foreach (string line in result.SplitMultilineText())
        {
            var parts = line.Split(':');
            switch (parts[0])
            {
                case "sec":
                    // New element starting
                    if (sec != null && fpr != null && uid != null)
                        yield return ParseSecretKey(sec, fpr, uid);
                    sec = parts;
                    fpr = null;
                    uid = null;
                    break;

                case "fpr":
                    fpr = parts;
                    break;

                case "uid":
                    uid = parts;
                    break;
            }
        }

        if (sec != null && fpr != null && uid != null)
            yield return ParseSecretKey(sec, fpr, uid);
    }

    private static OpenPgpSecretKey ParseSecretKey(string[] sec, string[] fpr, string[] uid)
        => new(
            OpenPgpUtils.ParseKeyID(sec[4]),
            OpenPgpUtils.ParseFingerprint(fpr[9]),
            UserID: uid[9]);

    /// <summary>
    /// Launches an interactive process for generating a new keypair.
    /// </summary>
    /// <returns>A handle that can be used to wait for the process to finish.</returns>
    /// <exception cref="IOException">The OpenPGP implementation could not be launched.</exception>
    public static Process GenerateKey() => new ProcessStartInfo
    {
        FileName =  "gpg",
        Arguments = "--gen-key"
    }.Start();
}
