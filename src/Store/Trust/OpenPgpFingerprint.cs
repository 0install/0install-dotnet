// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Trust;

/// <summary>
/// An OpenPGP key fingerprint.
/// </summary>
/// <param name="Identifier">A long identifier for an OpenPGP key.</param>
[Equatable]
public partial record OpenPgpFingerprint([property: OrderedEquality] byte[] Identifier)
{
    public static implicit operator byte[](OpenPgpFingerprint fingerprint) => fingerprint.Identifier;
}
