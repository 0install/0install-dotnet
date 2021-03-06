// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NanoByte.Common;
using NanoByte.Common.Streams;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Trust
{
    partial class GnuPG
    {
        /// <summary>
        /// Manages the interaction with the command-line interface of the external process.
        /// </summary>
        private class GpgProcess : ChildProcess
        {
            /// <inheritdoc/>
            protected override string AppBinary => "gpg";

            private static readonly object _gpgLock = new();

            private readonly string? _homeDir;

            private readonly ArraySegment<byte>? _stdinBytes;

            public GpgProcess(string? homeDir = null, ArraySegment<byte>? stdinBytes = null)
            {
                _homeDir = homeDir;
                _stdinBytes = stdinBytes;
            }

            /// <inheritdoc/>
            public override string Execute(params string[] arguments)
            {
                // Run only one gpg instance at a time to prevent file system race conditions
                lock (_gpgLock)
                    return base.Execute(arguments);
            }

            /// <inheritdoc/>
            protected override ProcessStartInfo GetStartInfo(params string[] arguments)
            {
                var startInfo = base.GetStartInfo(arguments);
                if (_homeDir != null) startInfo.EnvironmentVariables["GNUPGHOME"] = _homeDir;
                return startInfo;
            }

            protected override void InitStdin(StreamWriter writer)
            {
                #region Sanity checks
                if (writer == null) throw new ArgumentNullException(nameof(writer));
                #endregion

                if (_stdinBytes.HasValue)
                {
                    writer.BaseStream.Write(_stdinBytes.Value.Array!, _stdinBytes.Value.Offset, _stdinBytes.Value.Count);
                    writer.BaseStream.Flush();
                }
                writer.Close();
            }

            /// <inheritdoc/>
            protected override string? HandleStderr(string line)
            {
                #region Sanity checks
                if (line == null) throw new ArgumentNullException(nameof(line));
                #endregion

                if (line == "gpg: no valid OpenPGP data found.")
                    throw new InvalidDataException(line);
                if (line is "gpg: signing failed: secret key not available" or "gpg: WARNING: nothing exported")
                    throw new KeyNotFoundException(line);

                if (line.StartsWith("gpg: Signature made ") ||
                    line.StartsWith("gpg: Good signature from ") ||
                    line.StartsWith("gpg:                 aka") ||
                    line.StartsWith("gpg: WARNING: This key is not certified") ||
                    line.Contains("There is no indication") ||
                    line.StartsWith("Primary key fingerprint: ") ||
                    line.StartsWith("gpg: Can't check signature: public key not found"))
                    return null;

                if (line.StartsWith("gpg: BAD signature from ") ||
                    line.StartsWith("gpg: WARNING:") ||
                    (line.StartsWith("gpg: renaming ") && line.EndsWith("failed: Permission denied")))
                {
                    Log.Warn(line);
                    return null;
                }

                if (line.StartsWith("gpg: waiting for lock") ||
                    (line.StartsWith("gpg: keyring ") && line.EndsWith(" created")) ||
                    (line.StartsWith("gpg: ") && line.EndsWith(": trustdb created")))
                {
                    Log.Info(line);
                    return null;
                }

                if (line.StartsWith("gpg: skipped ") && line.EndsWith(": bad passphrase")) throw new WrongPassphraseException();
                if (line.StartsWith("gpg: signing failed: bad passphrase")) throw new WrongPassphraseException();
                if (line.StartsWith("gpg: signing failed: file exists")) throw new IOException(Resources.SignatureAldreadyExists);
                if (line.StartsWith("gpg: signing failed: ") ||
                    line.StartsWith("gpg: error") ||
                    line.StartsWith("gpg: critical"))
                    throw new IOException(line);

                // Unknown GnuPG message
                Log.Warn(line);
                return null;
            }
        }
    }
}
