// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// A compiled Java class file.
/// </summary>
public sealed class JavaClass : Java
{
    /// <inheritdoc/>
    internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
    {
        if (!base.Analyze(baseDirectory, file)) return false;
        if (!StringUtils.EqualsIgnoreCase(file.Extension, @".class")) return false;

        Name = file.Name[..^file.Extension.Length];
        GuiOnly = false;
        return true;
    }

    /// <inheritdoc/>
    public override Command CreateCommand() => new()
    {
        Name = CommandName,
        Path = RelativePath,
        Runner = new()
        {
            InterfaceUri = new("https://apps.0install.net/java/jre.xml"),
            Command = NeedsTerminal ? Command.NameRun : Command.NameRunGui,
            Versions = ToVersionRange(MinimumRuntimeVersion)
        }
    };
}
