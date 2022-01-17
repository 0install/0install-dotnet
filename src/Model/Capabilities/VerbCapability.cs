// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// Abstract base class for capabilities that can have multiple <see cref="Verb"/>s.
/// </summary>
[XmlType("verb-capability", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public abstract partial class VerbCapability : IconCapability
{
    /// <summary>
    /// A list of all available operations for the element.
    /// </summary>
    [Browsable(false)]
    [XmlElement("verb")]
    [OrderedEquality]
    public List<Verb> Verbs { get; } = new();

    #region Normalize
    /// <inheritdoc/>
    public override void Normalize()
    {
        base.Normalize();
        foreach (var verb in Verbs) verb.Normalize();
    }
    #endregion
}
