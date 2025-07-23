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
public sealed partial class BrowserNativeMessaging : Capability
{
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
    /// The browsers the native messaging host can be registered in.
    /// </summary>
    /// <seealso cref="KnownBrowsers"/>
    [Browsable(false)]
    [XmlIgnore]
    [OrderedEquality]
    public List<string> Browsers { get; } = [];

    /// <summary>Used for XML serialization.</summary>
    /// <seealso cref="Browsers"/>
    [TypeConverter(typeof(BrowserNameConverter))]
    [DisplayName(@"Browsers"), Description("Space-separated list of browsers the native messaging host can be registered in.")]
    [XmlAttribute("browser"), DefaultValue("")]
    [IgnoreEquality]
    public string BrowsersString
    {
        get => string.Join(" ", Browsers);
        set
        {
            Browsers.Clear();
            if (string.IsNullOrEmpty(value)) return;
            Browsers.Add(value.Split(' '));
        }
    }

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
    public override IEnumerable<string> ConflictIDs => Browsers.Select(browser => $"native-messaging:{browser}:{Name}");

    private static readonly Regex _nameRegex = new(@"^[a-z0-9._]+$"), _browserRegex = new(@"^[a-zA-Z0-9]+$");

    /// <inheritdoc/>
    public override void Normalize()
    {
        base.Normalize();

        EnsureAttribute(Name, "name");
        if (Name == null || Name.StartsWith(".") || Name.EndsWith(".") || Name.Contains("..") || !_nameRegex.IsMatch(Name))
            throw new InvalidDataException($"{string.Format(Resources.InvalidXmlAttributeOnTag, "name", ToShortXml())} Can only contain lowercase alphanumeric characters, underscores and dots. Can't start or end with a dot, and a dot can't be followed by another dot. {Resources.FoundInstead} {Name}");

        if (Browsers.Count == 0)
            throw new InvalidDataException(string.Format(Resources.MissingXmlAttributeOnTag, "browser", ToShortXml()));
        if (Browsers.Any(x => !_browserRegex.IsMatch(x)))
            throw new InvalidDataException($"{string.Format(Resources.InvalidXmlAttributeOnTag, "browser", ToShortXml())} Can only contain space-separated sequences of alphanumeric characters. {Resources.FoundInstead} {BrowsersString}");

        if (BrowserExtensions.Count == 0)
            throw new InvalidDataException(string.Format(Resources.MissingXmlTagInsideTag, "browser-extension", ToShortXml()));
        foreach (var extension in BrowserExtensions)
            extension.Normalize();
    }

    /// <summary>
    /// Returns the capability in the form "Browser: Name => Command". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => $"{Name} => {Command}";

    /// <inheritdoc/>
    public override Capability Clone() => new BrowserNativeMessaging
    {
        UnknownAttributes = UnknownAttributes,
        UnknownElements = UnknownElements,
        ID = ID,
        Browsers = {Browsers},
        Name = Name,
        BrowserExtensions = {BrowserExtensions.CloneElements()},
        Command = Command
    };
}
