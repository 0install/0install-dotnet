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
    private class GpgLauncher : ProcessLauncher
    {
        private readonly string? _homeDir;

        public GpgLauncher(string? homeDir = null)
            : base(fileName: "gpg", arguments: "--batch --no-secmem-warning")
        {
            _homeDir = homeDir;
        }

        /// <inheritdoc/>
        public override ProcessStartInfo GetStartInfo(params string[] arguments)
        {
            var startInfo = base.GetStartInfo(arguments);

            // Suppress localization to enable programmatic parsing of output
            startInfo.EnvironmentVariables["LANG"] = "C";

            if (_homeDir != null) startInfo.EnvironmentVariables["GNUPGHOME"] = _homeDir;

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

            if (line.StartsWith("gpg: Signature made ") ||
                line.StartsWith("gpg: Good signature from ") ||
                line.StartsWith("gpg:                 aka") ||
                line.StartsWith("gpg: WARNING: This key is not certified") ||
                line.Contains("There is no indication") ||
                line.StartsWith("Primary key fingerprint: ") ||
                line.StartsWith("gpg: Can't check signature: public key not found"))
                return;

            if (line.StartsWith("gpg: BAD signature from ") ||
                line.StartsWith("gpg: WARNING:") ||
                (line.StartsWith("gpg: renaming ") && line.EndsWith("failed: Permission denied")))
            {
                Log.Warn(line);
                return;
            }

            if (line.StartsWith("gpg: waiting for lock") ||
                (line.StartsWith("gpg: keyring ") && line.EndsWith(" created")) ||
                (line.StartsWith("gpg: ") && line.EndsWith(": trustdb created")))
            {
                Log.Info(line);
                return;
            }

            if (line.StartsWith("gpg: skipped ") && line.EndsWith(": bad passphrase")) throw new WrongPassphraseException();
            if (line.StartsWith("gpg: signing failed: bad passphrase")) throw new WrongPassphraseException();
            if (line.StartsWith("gpg: signing failed: file exists")) throw new IOException(Resources.SignatureAlreadyExists);
            if (line.StartsWith("gpg: signing failed: ") ||
                line.StartsWith("gpg: error") ||
                line.StartsWith("gpg: critical"))
                throw new IOException(line);

            // Unknown GnuPG message
            Log.Warn(line);
        }
    }
}
