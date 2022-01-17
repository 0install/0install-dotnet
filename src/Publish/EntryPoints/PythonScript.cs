// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// A script written in Python.
/// </summary>
public sealed class PythonScript : InterpretedScript
{
    /// <summary>
    /// Does this application have a graphical interface an no terminal output? Only enable if you are sure!
    /// </summary>
    [Category("Details (Python)"), DisplayName(@"GUI only"), Description("Does this application have a graphical interface an no terminal output? Only enable if you are sure!")]
    public bool GuiOnly { get => !NeedsTerminal; set => NeedsTerminal = !value; }

    /// <inheritdoc/>
    internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
    {
        if (!base.Analyze(baseDirectory, file)) return false;
        if (StringUtils.EqualsIgnoreCase(file.Extension, @".pyw"))
        {
            GuiOnly = true;
            return true;
        }
        else if (StringUtils.EqualsIgnoreCase(file.Extension, @".py") || HasShebang(file, "python"))
        {
            GuiOnly = false;
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    protected override FeedUri InterpreterInterface => new("https://apps.0install.net/python/python.xml");

    /// <inheritdoc/>
    public override Command CreateCommand() => new()
    {
        Name = CommandName,
        Path = RelativePath,
        Runner = new()
        {
            InterfaceUri = InterpreterInterface,
            Versions = InterpreterVersions,
            Command = NeedsTerminal ? Command.NameRun : Command.NameRunGui
        }
    };
}
