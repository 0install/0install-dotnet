// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

public enum PowerShellType
{
    Any,
    WindowsOnly,
    CoreOnly
}

/// <summary>
/// A script written in PowerShell.
/// </summary>
[Equatable]
public sealed partial class PowerShellScript : InterpretedScript
{
    /// <inheritdoc/>
    internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
        => base.Analyze(baseDirectory, file) && StringUtils.EqualsIgnoreCase(file.Extension, @".ps1");

    public override Command CreateCommand() => new()
    {
        Name = CommandName,
        Path = RelativePath,
        Runner = new()
        {
            InterfaceUri = InterpreterInterface,
            Versions = InterpreterVersions,
            Arguments = {"-ExecutionPolicy", "Bypass"}
        }
    };

    /// <inheritdoc/>
    protected override FeedUri InterpreterInterface
        => new(PowerShellType switch
        {
            PowerShellType.WindowsOnly => "https://apps.0install.net/powershell/windows.xml",
            PowerShellType.CoreOnly => "https://apps.0install.net/powershell/core.xml",
            _ => "https://apps.0install.net/powershell/powershell.xml",
        });

    /// <summary>
    /// The types of PowerShell supported by the script.
    /// </summary>
    [Category("Details (Script)"), DisplayName(@"PowerShell type"), Description("The types of PowerShell supported by the script.")]
    [DefaultValue(typeof(PowerShellType), "Any")]
    public PowerShellType PowerShellType { get; set; }
}
