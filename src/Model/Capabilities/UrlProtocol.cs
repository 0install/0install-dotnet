// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// An application's ability to handle a certain URL protocol such as HTTP.
/// </summary>
[Description("An application's ability to handle a certain URL protocol such as HTTP.")]
[Serializable, XmlRoot("url-protocol", Namespace = CapabilityList.XmlNamespace), XmlType("url-protocol", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public sealed partial class UrlProtocol : VerbCapability
{
    /// <summary>
    /// A well-known protocol prefix such as "http". Should be empty and set in <see cref="Capability.ID"/> instead if it is a custom protocol.
    /// </summary>
    [Browsable(false)]
    [XmlElement("known-prefix")]
    [OrderedEquality]
    public List<KnownProtocolPrefix> KnownPrefixes { get; } = new();

    /// <inheritdoc/>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public override IEnumerable<string> ConflictIDs => new[] {"progid:" + ID};

    #region Normalize
    /// <inheritdoc/>
    public override void Normalize()
    {
        base.Normalize();
        foreach (var prefix in KnownPrefixes) prefix.Normalize();
    }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the capability in the form "ID". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => $"{ID}";
    #endregion

    #region Clone
    /// <inheritdoc/>
    public override Capability Clone()
    {
        var capability = new UrlProtocol {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID, ExplicitOnly = ExplicitOnly};
        capability.Icons.AddRange(Icons);
        capability.Descriptions.AddRange(Descriptions.CloneElements());
        capability.Verbs.AddRange(Verbs.CloneElements());
        capability.KnownPrefixes.AddRange(KnownPrefixes);
        return capability;
    }
    #endregion
}
