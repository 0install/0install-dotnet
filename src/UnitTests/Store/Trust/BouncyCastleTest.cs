// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Runs test methods for <see cref="BouncyCastle"/>.
/// </summary>
public class BouncyCastleTest : OpenPgpTest, IDisposable
{
    private readonly TemporaryDirectory _homeDir = new("0install-unit-test");

    public void Dispose() => _homeDir?.Dispose();

    protected override IOpenPgp OpenPgp => new BouncyCastle(_homeDir);

    protected override void DeployKeyRings()
    {
        typeof(OpenPgpTest).CopyEmbeddedToFile("pubring.gpg", Path.Combine(_homeDir, "pubring.gpg"));
        typeof(OpenPgpTest).CopyEmbeddedToFile("secring.gpg", Path.Combine(_homeDir, "secring.gpg"));
    }
}
