// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Design;

namespace ZeroInstall.Model;

/// <summary>
/// A special kind of dependency: the program that is used to run this one. For example, a Python program might specify Python as its runner.
/// </summary>
/// <seealso cref="Model.Command.Runner"/>
[Description("A special kind of dependency: the program that is used to run this one. For example, a Python program might specify Python as its runner.")]
[Serializable, XmlRoot("runner", Namespace = Feed.XmlNamespace), XmlType("runner", Namespace = Feed.XmlNamespace)]
[Equatable]
public partial class Runner : Dependency, IArgBaseContainer
{
    /// <summary>
    /// The name of the command in the <see cref="Restriction.InterfaceUri"/> to use; leave <c>null</c> for <see cref="Model.Command.NameRun"/>.
    /// </summary>
    [Description("The name of the command in the interface to use; leave empty for 'run'.")]
    [TypeConverter(typeof(CommandNameConverter))]
    [XmlAttribute("command"), DefaultValue("")]
    public string? Command { get; set; }

    /// <summary>
    /// A list of command-line arguments to be passed to the runner before the path of the implementation.
    /// </summary>
    [Browsable(false)]
    [XmlElement(typeof(Arg)), XmlElement(typeof(ForEachArgs))]
    [OrderedEquality]
    public List<ArgBase> Arguments { get; } = new();

    #region Normalize
    protected override string XmlTagName => "runner";

    /// <inheritdoc/>
    public override void Normalize()
    {
        base.Normalize();

        foreach (var argument in Arguments) argument.Normalize();
    }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the runner in the form "Interface (Command)". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"{InterfaceUri} ({Command ?? Model.Command.NameRun})";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="Runner"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Runner"/>.</returns>
    public Runner CloneRunner() => new()
    {
        InterfaceUri = InterfaceUri,
        Use = Use,
        Command = Command,
        Versions = Versions,
        Bindings = {Bindings.CloneElements()},
        Constraints = {Constraints.CloneElements()},
        Arguments = {Arguments.CloneElements()}
    };

    /// <summary>
    /// Creates a deep copy of this <see cref="Runner"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Runner"/>.</returns>
    public override Restriction Clone() => CloneRunner();
    #endregion
}
