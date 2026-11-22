// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;

namespace ZeroInstall.Store.Trust;

partial class GnuPG
{
    /// <summary>
    /// Manages the interaction with the command-line interface of the gpg process.
    /// </summary>
    private class GpgLauncher(string? homeDir = null)
        : ProcessLauncher(fileName: "gpg", arguments: "--batch --no-secmem-warning")
    {
        /// <inheritdoc/>
        public override ProcessStartInfo GetStartInfo(params string[] arguments)
        {
            var startInfo = base.GetStartInfo(arguments);

            // Suppress localization to enable programmatic parsing of output
            startInfo.EnvironmentVariables["LANG"] = "C";

            if (homeDir != null) startInfo.EnvironmentVariables["GNUPGHOME"] = homeDir;

            return startInfo;
        }

        /// <inheritdoc/>
        protected override void OnStderr(string line, StreamWriter stdin)
        {
            switch (line)
            {
                case "gpg: no valid OpenPGP data found.":
                    throw new InvalidDataException(line);
                case "gpg: signing failed: secret key not available":
                case "gpg: WARNING: nothing exported":
                    throw new KeyNotFoundException(line);
            }

            if (line.StartsWithOrdinal("gpg: Signature made ") ||
                line.StartsWithOrdinal("gpg: Good signature from ") ||
                line.StartsWithOrdinal("gpg:                 aka") ||
                line.StartsWithOrdinal("gpg: WARNING: This key is not certified") ||
                line.Contains("There is no indication") ||
                line.StartsWithOrdinal("Primary key fingerprint: ") ||
                line.StartsWithOrdinal("gpg: Can't check signature: public key not found"))
                return;

            if (line.StartsWithOrdinal("gpg: BAD signature from ") ||
                line.StartsWithOrdinal("gpg: WARNING:") ||
                (line.StartsWithOrdinal("gpg: renaming ") && line.EndsWithOrdinal("failed: Permission denied")))
            {
                Log.Warn(line);
                return;
            }

            if (line.StartsWithOrdinal("gpg: waiting for lock") ||
                (line.StartsWithOrdinal("gpg: keyring ") && line.EndsWithOrdinal(" created")) ||
                (line.StartsWithOrdinal("gpg: ") && line.EndsWithOrdinal(": trustdb created")))
            {
                Log.Info(line);
                return;
            }

            if (line.StartsWithOrdinal("gpg: skipped ") && line.EndsWithOrdinal(": bad passphrase")) throw new WrongPassphraseException();
            if (line.StartsWithOrdinal("gpg: signing failed: bad passphrase")) throw new WrongPassphraseException();
            if (line.StartsWithOrdinal("gpg: signing failed: file exists")) throw new IOException(Resources.SignatureAlreadyExists);
            if (line.StartsWithOrdinal("gpg: signing failed: ") ||
                line.StartsWithOrdinal("gpg: error") ||
                line.StartsWithOrdinal("gpg: critical"))
                throw new IOException(line);

            // Unknown GnuPG message
            Log.Warn(line);
        }
    }
}
