// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Design;

namespace ZeroInstall.Model;

/// <summary>
/// A command says how to run an <see cref="Implementation"/> as a program.
/// </summary>
/// <seealso cref="Element.Commands"/>
[Description("A command says how to run an implementation as a program.")]
[Serializable, XmlRoot("command", Namespace = Feed.XmlNamespace), XmlType("command", Namespace = Feed.XmlNamespace)]
[Equatable]
public partial class Command : FeedElement, IArgBaseContainer, IBindingContainer, IDependencyContainer, ICloneable<Command>
{
    #region Constants
    /// <summary>
    /// Canonical <see cref="Name"/> corresponding to <see cref="Element.Main"/>.
    /// </summary>
    public const string NameRun = "run";

    /// <summary>
    /// Conventional <see cref="Name"/> for GUI-only versions of applications.
    /// </summary>
    public const string NameRunGui = "run-gui";

    /// <summary>
    /// Canonical <see cref="Name"/> corresponding to <see cref="Element.SelfTest"/>.
    /// </summary>
    public const string NameTest = "test";

    /// <summary>
    /// Canonical <see cref="Name"/> used by 0compile.
    /// </summary>
    public const string NameCompile = "compile";
    #endregion

    /// <summary>
    /// The name of the command. Well-known names are <see cref="NameRun"/>, <see cref="NameTest"/> and <see cref="NameCompile"/>.
    /// </summary>
    [Description("The name of the command.")]
    [TypeConverter(typeof(CommandNameConverter))]
    [XmlAttribute("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The relative path of an executable inside the implementation that should be executed to run this command.
    /// </summary>
    [Description("The relative path of an executable inside the implementation that should be executed to run this command.")]
    [XmlAttribute("path")]
    public string? Path { get; set; }

    /// <summary>
    /// A list of command-line arguments to be passed to an implementation executable.
    /// </summary>
    [Browsable(false)]
    [XmlElement(typeof(Arg)), XmlElement(typeof(ForEachArgs))]
    [OrderedEquality]
    public List<ArgBase> Arguments { get; } = [];

    /// <summary>
    /// A list of <see cref="Binding"/>s for <see cref="Implementation"/>s to locate <see cref="Dependency"/>s.
    /// </summary>
    [Browsable(false)]
    [XmlElement(typeof(GenericBinding)), XmlElement(typeof(EnvironmentBinding)), XmlElement(typeof(OverlayBinding)), XmlElement(typeof(ExecutableInVar)), XmlElement(typeof(ExecutableInPath))]
    [OrderedEquality]
    public List<Binding> Bindings { get; } = [];

    /// <summary>
    /// Switches the working directory of a process on startup to a location within an implementation.
    /// </summary>
    [Browsable(false)]
    [XmlElement("working-dir")]
    public WorkingDir? WorkingDir { get; set; }

    /// <summary>
    /// A list of interfaces this command depends upon.
    /// </summary>
    [Browsable(false)]
    [XmlElement("requires")]
    [OrderedEquality]
    public List<Dependency> Dependencies { get; } = [];

    /// <summary>
    /// A list of interfaces that are restricted to specific versions when used.
    /// </summary>
    [Browsable(false)]
    [XmlElement("restricts")]
    [OrderedEquality]
    public List<Restriction> Restrictions { get; } = [];

    /// <summary>
    /// A special kind of dependency: the program that is used to run this one. For example, a Python program might specify Python as its runner.
    /// </summary>
    [Browsable(false)]
    [XmlElement("runner")]
    public Runner? Runner { get; set; }

    #region Normalize
    /// <summary>
    /// Converts legacy elements, sets default values, etc..
    /// </summary>
    /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
    public virtual void Normalize()
    {
        EnsureAttribute(Name, "name");

        // Apply if-0install-version filter
        Arguments.RemoveAll(FilterMismatch);
        Dependencies.RemoveAll(FilterMismatch);
        Restrictions.RemoveAll(FilterMismatch);
        Bindings.RemoveAll(FilterMismatch);
        if (FilterMismatch(WorkingDir)) WorkingDir = null;

        foreach (var argument in Arguments) argument.Normalize();
        foreach (var binding in Bindings) binding.Normalize();
        Runner?.Normalize();
        foreach (var dependency in Dependencies) dependency.Normalize();
        foreach (var restriction in Restrictions) restriction.Normalize();
    }
    #endregion

    #region Conversion
    /// <summary>
    /// Returns the Command in the form "Name (Path)". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"{Name} ({Path})";
    #endregion

    #region Clone
    /// <summary>
    /// Creates a deep copy of this <see cref="Command"/> instance.
    /// </summary>
    /// <returns>The new copy of the <see cref="Command"/>.</returns>
    public Command Clone() => new()
    {
        UnknownAttributes = UnknownAttributes,
        UnknownElements = UnknownElements,
        IfZeroInstallVersion = IfZeroInstallVersion,
        Name = Name,
        Path = Path,
        WorkingDir = WorkingDir?.Clone(),
        Runner = Runner?.CloneRunner(),
        Arguments = {Arguments.CloneElements()},
        Bindings = {Bindings.CloneElements()},
        Dependencies = {Dependencies.CloneElements()},
        Restrictions = {Restrictions.CloneElements()}
    };
    #endregion
}
