// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Design;

namespace ZeroInstall.Model.Capabilities;

/// <summary>
/// A hook/callback into the application to be called during <c>0install remove</c>.
/// </summary>
[Description("A hook/callback into the application to be called during '0install remove'.")]
[Serializable, XmlRoot("remove-hook", Namespace = CapabilityList.XmlNamespace), XmlType("remove-hook", Namespace = CapabilityList.XmlNamespace)]
[Equatable]
public sealed partial class RemoveHook : Capability
{
    /// <summary>
    /// The name of the command in the <see cref="Feed"/> to use when a removal of the app is requested; leave <c>null</c> for <see cref="Model.Command.NameRun"/>.
    /// </summary>
    [Description("The name of the command in the feed to use when a removal of the app is requested; leave empty for 'run'.")]
    [TypeConverter(typeof(CommandNameConverter))]
    [XmlAttribute("command"), DefaultValue("")]
    public string? Command { get; set; }

    /// <summary>
    /// Command-line arguments to be passed to the command. Will be automatically escaped to allow proper concatenation of multiple arguments containing spaces.
    /// </summary>
    [Browsable(false)]
    [XmlElement("arg")]
    [OrderedEquality]
    public List<Arg> Arguments { get; } = new();

    /// <inheritdoc/>
    [Browsable(false), XmlIgnore, IgnoreEquality]
    public override IEnumerable<string> ConflictIDs => Enumerable.Empty<string>();

    #region Conversion
    /// <summary>
    /// Returns the capability in the form "Command". Not safe for parsing!
    /// </summary>
    public override string ToString()
        => $"{Command}";
    #endregion

    #region Clone
    /// <inheritdoc/>
    public override Capability Clone()
    {
        var capability = new RemoveHook {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, ID = ID, Command = Command};
        capability.Arguments.AddRange(Arguments.CloneElements());
        return capability;
    }
    #endregion
}
