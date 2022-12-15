// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Makes an application the default handler for something.
/// </summary>
/// <seealso cref="Model.Capabilities.Capability"/>
[XmlType(TagName, Namespace = AppList.XmlNamespace)]
[Equatable]
public abstract partial class DefaultAccessPoint : AccessPoint
{
    public const string TagName = "default-access-point", AltName = "default-app";

    /// <summary>
    /// The ID of the <see cref="Capability"/> to be made the default handler.
    /// </summary>
    [Description("The ID of the Capability to be made the default handler.")]
    [XmlAttribute("capability")]
    public required string Capability { get; set; }
}
