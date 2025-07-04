// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text.RegularExpressions;
using ZeroInstall.Model.Design;

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// An application's ability to act as a browser native messaging host.
/// </summary>
[Description("An application's ability to act as a browser native messaging host.")]
[Serializable, XmlRoot("native-messaging", Namespace = CapabilityList.XmlNamespace), XmlType("native-messaging", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public sealed partial class BrowserNativeMessaging : DefaultCapability
{
    /// <summary>
    /// The browser the native messaging host can be registered in.
    /// </summary>
    [Description("The browser the native messaging host can be registered in.")]
    [XmlAttribute("browser")]
    public required Browser Browser { get; set; }

    /// <summary>
    /// The name used to call the native messaging host from browser extensions.
    /// </summary>
    /// <remarks>Can only contain lowercase alphanumeric characters, underscores and dots. Can't start or end with a dot, and a dot can't be followed by another dot.</remarks>
    [Description("""
                 The name used to call the native messaging host from browser extensions.
                 Can only contain lowercase alphanumeric characters, underscores and dots. Can't start or end with a dot, and a dot can't be followed by another dot.
                 """)]
    [XmlAttribute("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The name of the command in the <see cref="Feed"/> to use; leave <c>null</c> for <see cref="Model.Command.NameRun"/>.
    /// </summary>
    [Description("The name of the command in the feed to use; leave empty for 'run'.")]
    [TypeConverter(typeof(CommandNameConverter))]
    [XmlAttribute("command"), DefaultValue("")]
    public string? Command { get; set; }

    /// <summary>
    /// List of browser extensions that should have access to the native messaging host.
    /// </summary>
    [Browsable(false)]
    [XmlElement("browser-extension")]
    [OrderedEquality]
    public List<BrowserExtension> BrowserExtensions { get; } = [];

    /// <inheritdoc/>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public override IEnumerable<string> ConflictIDs => [$"native-messaging:{Browser}:{Name}"];

    private static readonly Regex _nameRegex = new(@"^[a-z0-9._]+$");

    /// <inheritdoc/>
    public override void Normalize()
    {
        base.Normalize();

        if (string.IsNullOrEmpty(Name) || Name.StartsWith(".") || Name.EndsWith(".") || Name.Contains("..") || !_nameRegex.IsMatch(Name))
            throw new InvalidDataException($"{string.Format(Resources.InvalidXmlAttributeOnTag, "name", ToShortXml())} Can only contain lowercase alphanumeric characters, underscores and dots. Can't start or end with a dot, and a dot can't be followed by another dot. {Resources.FoundInstead} {Name}");
    }

    /// <summary>
    /// Returns the capability in the form "Browser: Name => Command". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => $"{Browser}: {Name} => {Command}";

    /// <inheritdoc/>
    public override Capability Clone() => new BrowserNativeMessaging
    {
        UnknownAttributes = UnknownAttributes,
        UnknownElements = UnknownElements,
        ID = ID,
        ExplicitOnly = ExplicitOnly,
        Browser = Browser,
        Name = Name,
        BrowserExtensions = {BrowserExtensions.CloneElements()},
        Command = Command
    };
}
