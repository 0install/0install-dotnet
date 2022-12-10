// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// Names a well-known protocol prefix. Used for protocols that are shared across many applications (e.g. HTTP, FTP) but not for application-specific protocols.
/// </summary>
/// <seealso cref="UrlProtocol.KnownPrefixes"/>
[Description("Names a well-known protocol prefix. Used for protocols that are shared across many applications (e.g. HTTP, FTP) but not for application-specific protocols.")]
[Serializable, XmlRoot("known-prefix", Namespace = CapabilityList.XmlNamespace), XmlType("known-prefix", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public partial class KnownProtocolPrefix : XmlUnknown, ICloneable<KnownProtocolPrefix>
{
    /// <summary>
    /// The value of the prefix (e.g. "http").
    /// </summary>
    [Description("""The value of the prefix (e.g. "http").""")]
    [XmlAttribute("value")]
    public string Value { get; set; } = default!;

    #region Normalize
    /// <summary>
    /// Converts legacy elements, sets default values, etc..
    /// </summary>
    /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
    public void Normalize()
        => EnsureAttributeSafeID(Value, "value");
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the prefix in the form "Value". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => $"{Value}";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="KnownProtocolPrefix"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="KnownProtocolPrefix"/>.</returns>
    public KnownProtocolPrefix Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Value = Value};
    #endregion
}
