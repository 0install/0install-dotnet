// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Trust;

/// <summary>
/// A known OpenPGP key, trusted to sign feeds from a certain set of domains.
/// </summary>
[XmlType("key", Namespace = TrustDB.XmlNamespace)]
[Equatable]
public sealed partial class Key : ICloneable<Key>
{
    /// <summary>
    /// The cryptographic fingerprint of this key.
    /// </summary>
    [XmlAttribute("fingerprint")]
    public string? Fingerprint { get; set; }

    /// <summary>
    /// A list of <see cref="Domain"/>s this key is valid for.
    /// </summary>
    [XmlElement("domain")]
    [SetEquality]
    public DomainSet Domains { get; } = new();

    #region Conversion
    /// <summary>
    /// Returns the key in the form "Fingerprint: Domain1, Domain2, ...". Safe for parsing!
    /// </summary>
    public override string ToString()
        => $"{Fingerprint}: {Domains}";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="Key"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Key"/>.</returns>
    public Key Clone() => new()
    {
        Fingerprint = Fingerprint,
        Domains = {Domains.CloneElements()}
    };
    #endregion
}
