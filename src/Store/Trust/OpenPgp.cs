// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Provides <see cref="IOpenPgp"/> instances.
/// </summary>
public static class OpenPgp
{
    /// <summary>
    /// Creates an instance of <see cref="IOpenPgp"/> intended for verifying signatures.
    /// </summary>
    public static IOpenPgp Verifying()
        => new BouncyCastle(VerifyingHomeDir);

    /// <summary>
    /// The directory containing key rings used for verifying signatures.
    /// </summary>
    /// <remarks>This is different from the normal GnuPG home directory to avoid polluting user profile with auto-imported public keys.</remarks>
    public static string VerifyingHomeDir
        => Locations.GetCacheDirPath("0install.net", machineWide: false);

    /// <summary>
    /// Creates an instance of <see cref="IOpenPgp"/> intended for creating signatures.
    /// </summary>
    public static IOpenPgp Signing()
        => new BouncyCastle(SigningHomeDir);

    /// <summary>
    /// The directory containing key rings used for creating signatures.
    /// </summary>
    /// <remarks>This matches the normal GnuPG home directory.</remarks>
    public static string SigningHomeDir
        => Environment.GetEnvironmentVariable("GNUPGHOME")
        ?? (WindowsUtils.IsWindows
               ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gnupg")
               : Path.Combine(Locations.HomeDir, ".gnupg"));
}
