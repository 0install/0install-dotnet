// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// Groups a number of application <see cref="Capability"/>s (for a specific operating system) that can be registered in a desktop environment.
/// </summary>
[Description("Groups a number of application capabilities (for a specific operating system) that can be registered in a desktop environment.")]
[Serializable, XmlRoot("capabilities", Namespace = XmlNamespace), XmlType("capabilities", Namespace = XmlNamespace)]
[Equatable]
public sealed partial class CapabilityList : XmlUnknown, ICloneable<CapabilityList>
{
    #region Constants
    /// <summary>
    /// The XML namespace used for storing application capabilities.
    /// </summary>
    public const string XmlNamespace = "http://0install.de/schema/desktop-integration/capabilities";

    /// <summary>
    /// The URI to retrieve an XSD containing the XML Schema information for this class in serialized form.
    /// </summary>
    public const string XsdLocation = "https://docs.0install.net/specifications/capabilities.xsd";
    #endregion

    /// <summary>
    /// Determines for which operating system the <see cref="Capability"/>s are applicable.
    /// </summary>
    [Description("Determines for which operating system the capabilities are applicable.")]
    [XmlAttribute("os"), DefaultValue(typeof(OS), "All")]
    public OS OS { get; set; }

    /// <summary>
    /// A list of <see cref="Capability"/>s.
    /// </summary>
    [Browsable(false)]
    [XmlElement(typeof(AppRegistration)),
     XmlElement(typeof(FileType)),
     XmlElement(typeof(UrlProtocol)),
     XmlElement(typeof(ContextMenu)),
     XmlElement(typeof(AutoPlay)),
     XmlElement(typeof(ComServer)),
     XmlElement(typeof(DefaultProgram)),
     XmlElement(typeof(RemoveHook))]
    [OrderedEquality]
    public List<Capability> Entries { get; } = new();

    /// <summary>
    /// Retrieves the first <see cref="Capability"/> that matches a specific type and ID. Safe for missing elements.
    /// </summary>
    /// <typeparam name="T">The capability type to match.</typeparam>
    /// <param name="id">The <see cref="Capability.ID"/> to match.</param>
    /// <returns>The first matching <see cref="Capability"/>; <c>null</c> if no match was found.</returns>
    /// <exception cref="KeyNotFoundException">No capability matching <paramref name="id"/> and <typeparamref name="T"/> was found.</exception>
    public T? GetCapability<T>(string id) where T : Capability
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        #endregion

        return Entries.OfType<T>().FirstOrDefault(specificCapability => specificCapability.ID == id);
    }

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="CapabilityList"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="CapabilityList"/>.</returns>
    public CapabilityList Clone() => new()
    {
        UnknownAttributes = UnknownAttributes,
        UnknownElements = UnknownElements,
        OS = OS,
        Entries = {Entries.CloneElements()}
    };
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the capability list in the form "OS". Not safe for parsing!
    /// </summary>
    public override string ToString() => OS.ToString();
    #endregion
}
