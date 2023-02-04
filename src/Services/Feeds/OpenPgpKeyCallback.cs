// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Services.Feeds;

/// <summary>
/// Callback for reading a specific OpenPGP public key file.
/// </summary>
/// <param name="id">The key ID as a canonical string..</param>
/// <returns>The public key in binary or ASCII Armored format.</returns>
public delegate ArraySegment<byte>? OpenPgpKeyCallback(string id);
