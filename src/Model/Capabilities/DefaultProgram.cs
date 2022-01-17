// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// Can act as the default provider for a well-known service such web-browser, e-mail client.
/// </summary>
[Description("Can act as the default provider for a well-known service such web-browser, e-mail client.")]
[Serializable, XmlType("default-program", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public sealed partial class DefaultProgram : VerbCapability
{
    #region Constants
    /// <summary>
    /// Canonical <see cref="Service"/> for web browsers.
    /// </summary>
    public const string ServiceInternet = "StartMenuInternet";

    /// <summary>
    /// Canonical <see cref="Service"/> for mail clients.
    /// </summary>
    public const string ServiceMail = "Mail";

    /// <summary>
    /// Canonical <see cref="Service"/> for media players.
    /// </summary>
    public const string ServiceMedia = "Media";

    /// <summary>
    /// Canonical <see cref="Service"/> for instant messengers.
    /// </summary>
    public const string ServiceMessenger = "IM";

    /// <summary>
    /// Canonical <see cref="Service"/> for Java Virtual Machines.
    /// </summary>
    public const string ServiceJava = "JVM";

    /// <summary>
    /// Canonical <see cref="Service"/> for calender tools.
    /// </summary>
    public const string ServiceCalender = "Calender";

    /// <summary>
    /// Canonical <see cref="Service"/> for address books.
    /// </summary>
    public const string ServiceContacts = "Contacts";

    /// <summary>
    /// Canonical <see cref="Service"/> for internet call tools.
    /// </summary>
    public const string ServiceInternetCall = "Internet Call";
    #endregion

    /// <inheritdoc/>
    [XmlIgnore]
    public override bool WindowsMachineWideOnly => true;

    /// <summary>
    /// The name of the service the application provides.
    /// Well-known values on Windows are: Mail, Media, IM, JVM, Calender, Contacts, Internet Call
    /// </summary>
    [Description("The name of the service the application provides. Well-known values on Windows are: Mail, Media, IM, JVM, Calender, Contacts, Internet Call")]
    [XmlAttribute("service")]
    public string Service { get; set; } = default!;

    /// <summary>
    /// Lists the commands the application registers for use by Windows' "Set Program Access and Defaults". Will be transparently replaced with Zero Install commands at runtime.
    /// </summary>
    /// <remarks>These strings are used for registry filtering. They are never actually executed.</remarks>
    [Description("Lists the commands the application registers for use by Windows' \"Set Program Access and Defaults\". Will be transparently replaced with Zero Install commandss at runtime.")]
    [XmlElement("install-commands")]
    public InstallCommands InstallCommands { get; set; }

    /// <inheritdoc/>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public override IEnumerable<string> ConflictIDs => new[] {"clients:" + Service + @"\" + ID};

    #region Normalize
    /// <inheritdoc/>
    public override void Normalize()
    {
        base.Normalize();
        EnsureAttribute(Service, "service");
        if (Service.Contains(@"\"))
            throw new InvalidDataException($"Invalid 'service' attribute on {ToShortXml()}. Should not contain backslashes but was: {Service}");
    }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the capability in the form "Service (ID)". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => $"{Service} ({ID})";
    #endregion

    #region Clone
    /// <inheritdoc/>
    public override Capability Clone()
    {
        var capability = new DefaultProgram {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID, ExplicitOnly = ExplicitOnly, Service = Service, InstallCommands = InstallCommands};
        capability.Descriptions.AddRange(Descriptions.CloneElements());
        capability.Icons.AddRange(Icons);
        capability.Verbs.AddRange(Verbs.CloneElements());
        return capability;
    }
    #endregion
}
