// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Publish.EntryPoints.Design;

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// A compiled Java application.
/// </summary>
[Equatable]
public abstract partial class Java : Candidate
{
    /// <summary>
    /// The minimum version of the Java Runtime Environment required by the application.
    /// </summary>
    [Category("Details (Java)"), DisplayName(@"Minimum Java version"), Description("The minimum version of the Java Runtime Environment required by the application.")]
    [DefaultValue("")]
    [TypeConverter(typeof(JavaVersionConverter))]
    public ImplementationVersion? MinimumRuntimeVersion { get; set; }

    /// <summary>
    /// Does this application have external dependencies that need to be injected by Zero Install? Only enable if you are sure!
    /// </summary>
    [Category("Details (Java)"), DisplayName(@"External dependencies"), Description("Does this application have external dependencies that need to be injected by Zero Install? Only enable if you are sure!")]
    [DefaultValue(false)]
    public bool ExternalDependencies { get; set; }

    /// <summary>
    /// Does this application have a graphical interface an no terminal output? Only enable if you are sure!
    /// </summary>
    [Category("Details (Java)"), DisplayName(@"GUI only"), Description("Does this application have a graphical interface an no terminal output? Only enable if you are sure!")]
    public bool GuiOnly { get => !NeedsTerminal; set => NeedsTerminal = !value; }
}
