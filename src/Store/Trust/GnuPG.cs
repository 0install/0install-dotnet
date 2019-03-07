// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// Provides access to the signature functions of GnuPG.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public partial class GnuPG : IOpenPgp
    {
        /// <inheritdoc/>
        public IEnumerable<OpenPgpSignature> Verify(byte[] data, byte[] signature)
        {
            #region Sanity checks
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (signature == null) throw new ArgumentNullException(nameof(signature));
            #endregion

            string result;
            using (var signatureFile = new TemporaryFile("0install-sig"))
            {
                File.WriteAllBytes(signatureFile, signature);
                result = new CliControl(HomeDir, data).Execute("--batch", "--no-secmem-warning", "--status-fd", "1", "--verify", signatureFile.Path, "-");
            }
            var lines = result.SplitMultilineText();

            // Each signature is represented by one line of encoded information
            var signatures = new List<OpenPgpSignature>(lines.Length);
            foreach (string line in lines)
            {
                try
                {
                    var parsedSignature = ParseSignatureLine(line);
                    if (parsedSignature != null) signatures.Add(parsedSignature);
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
        [CanBeNull]
        private static OpenPgpSignature ParseSignatureLine([NotNull] string line)
        {
            const int signatureTypeIndex = 1, fingerprintIndex = 2, timestampIndex = 4, keyIDIndex = 2, errorCodeIndex = 7;

            var signatureParts = line.Split(' ');
            if (signatureParts.Length < signatureTypeIndex + 1) return null;
            switch (signatureParts[signatureTypeIndex])
            {
                case "VALIDSIG":
                    if (signatureParts.Length != 12) throw new FormatException("Incorrect number of columns in VALIDSIG line.");
                    var fingerprint = OpenPgpUtils.ParseFingerpint(signatureParts[fingerprintIndex]);
                    return new ValidSignature(
                        keyID: OpenPgpUtils.FingerprintToKeyID(fingerprint),
                        fingerprint: fingerprint,
                        timestamp: FileUtils.FromUnixTime(long.Parse(signatureParts[timestampIndex])));

                case "BADSIG":
                    if (signatureParts.Length < 3) throw new FormatException("Incorrect number of columns in BADSIG line.");
                    return new BadSignature(OpenPgpUtils.ParseKeyID(signatureParts[keyIDIndex]));

                case "ERRSIG":
                    if (signatureParts.Length != 8) throw new FormatException("Incorrect number of columns in ERRSIG line.");
                    int errorCode = int.Parse(signatureParts[errorCodeIndex]);
                    switch (errorCode)
                    {
                        case 9:
                            return new MissingKeySignature(OpenPgpUtils.ParseKeyID(signatureParts[keyIDIndex]));
                        default:
                            return new ErrorSignature(OpenPgpUtils.ParseKeyID(signatureParts[keyIDIndex]));
                    }

                default:
                    return null;
            }
        }

        /// <inheritdoc/>
        public byte[] Sign(byte[] data, OpenPgpSecretKey secretKey, string passphrase = null)
        {
            #region Sanity checks
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (secretKey == null) throw new ArgumentNullException(nameof(secretKey));
            #endregion

            return Convert.FromBase64String(
                new CliControl(HomeDir, data)
                   .Execute("--batch", "--no-secmem-warning", "--passphrase", passphrase ?? "", "--local-user", secretKey.FormatKeyID(), "--detach-sign", "--armor", "--output", "-", "-")
                   .GetRightPartAtFirstOccurrence(Environment.NewLine + Environment.NewLine)
                   .GetLeftPartAtLastOccurrence(Environment.NewLine + "=")
                   .Replace(Environment.NewLine, "\n"));
        }

        /// <inheritdoc/>
        public void ImportKey(byte[] data)
        {
            #region Sanity checks
            if (data == null) throw new ArgumentNullException(nameof(data));
            #endregion

            new CliControl(HomeDir, data).Execute("--batch", "--no-secmem-warning", "--quiet", "--import");
        }

        /// <inheritdoc/>
        public string ExportKey(IKeyIDContainer keyIDContainer)
        {
            #region Sanity checks
            if (keyIDContainer == null) throw new ArgumentNullException(nameof(keyIDContainer));
            #endregion

            string result = new CliControl(HomeDir).Execute("--batch", "--no-secmem-warning", "--armor", "--export", keyIDContainer.FormatKeyID());
            return result.Replace(Environment.NewLine, "\n") + "\n";
        }

        /// <inheritdoc/>
        public IEnumerable<OpenPgpSecretKey> ListSecretKeys()
        {
            string result = new CliControl(HomeDir).Execute("--batch", "--no-secmem-warning", "--list-secret-keys", "--with-colons", "--fixed-list-mode", "--fingerprint");

            string[] sec = null, fpr = null, uid = null;
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
            => new OpenPgpSecretKey(
                keyID: OpenPgpUtils.ParseKeyID(sec[4]),
                fingerprint: OpenPgpUtils.ParseFingerpint(fpr[9]),
                userID: uid[9]);

        /// <inheritdoc/>
        public string HomeDir { get; set; } = DefaultHomeDir;

        /// <summary>
        /// The default value for <see cref="HomeDir"/> based on the current operating system and environment variables.
        /// </summary>
        public static string DefaultHomeDir =>
            Environment.GetEnvironmentVariable("GNUPGHOME") ??
            (WindowsUtils.IsWindows
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gnupg")
                : Path.Combine(Locations.HomeDir, ".gnupg"));

        /// <summary>
        /// Launches an interactive process for generating a new keypair.
        /// </summary>
        /// <returns>A handle that can be used to wait for the process to finish.</returns>
        /// <exception cref="IOException">The OpenPGP implementation could not be launched.</exception>
        [PublicAPI]
        public static Process GenerateKey() => new CliControl().StartInteractive("--gen-key");
    }
}
