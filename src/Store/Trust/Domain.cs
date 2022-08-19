// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Trust;

/// <summary>
/// A specific domain with feeds a <see cref="Key"/> is trusted to sign.
/// </summary>
/// <param name="Value">A valid domain name (not a full <see cref="Uri"/>!).</param>
[XmlType("domain", Namespace = TrustDB.XmlNamespace)]
public record struct Domain([property: XmlAttribute("value")] string? Value = null)
{
    /// <inheritdoc/>
    public override string? ToString() => Value;
}
