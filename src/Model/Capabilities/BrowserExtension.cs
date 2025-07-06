// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// A browser extension.
/// </summary>
[Description("A browser extension.")]
[Serializable, XmlRoot("browser-extension", Namespace = CapabilityList.XmlNamespace), XmlType("browser-extension", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public partial class BrowserExtension : XmlUnknown, ICloneable<BrowserExtension>
{
    /// <summary>
    /// The ID of the browser extension, without prefixes like chrome-extension://.
    /// </summary>
    [Description("The ID of the browser extension, without prefixes like chrome-extension://.")]
    [XmlAttribute("id")]
    public required string ID { get; set; }

    /// <summary>
    /// Converts legacy elements, sets default values, etc..
    /// </summary>
    /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
    public void Normalize()
        => EnsureAttribute(ID, "id");

    /// <summary>
    /// Returns the extension in the form "ID". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => $"{ID}";

    /// <summary>
    /// Creates a deep copy of this <see cref="BrowserExtension"/> instance.
    /// </summary>
    public BrowserExtension Clone() => new() {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID};
}
