// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common;
using ZeroInstall.Model;

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// A Java JAR archive.
/// </summary>
public sealed class JavaJar : Java
{
    /// <inheritdoc/>
    internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
    {
        if (!base.Analyze(baseDirectory, file)) return false;
        if (!StringUtils.EqualsIgnoreCase(file.Extension, @".jar")) return false;

        // TODO: Parse JAR metadata
        Name = file.Name[..^file.Extension.Length];
        GuiOnly = false;
        return true;
    }

    /// <inheritdoc/>
    public override Command CreateCommand() => ExternalDependencies
        ? new Command
        {
            Name = CommandName,
            Bindings = {new EnvironmentBinding {Name = "CLASSPATH", Insert = RelativePath}},
            Runner = new()
            {
                InterfaceUri = new("https://apps.0install.net/java/jar-launcher.xml"),
                Command = NeedsTerminal ? Command.NameRun : Command.NameRunGui,
                Versions = ToVersionRange(MinimumRuntimeVersion)
            }
        }
        : new Command
        {
            Name = CommandName,
            Path = RelativePath,
            Runner = new()
            {
                InterfaceUri = new("https://apps.0install.net/java/jre.xml"),
                Command = NeedsTerminal ? Command.NameRun : Command.NameRunGui,
                Arguments = {@"-jar"},
                Versions = ToVersionRange(MinimumRuntimeVersion)
            }
        };
}
