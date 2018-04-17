// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.EntryPoints
{
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
            Name = file.Name.Substring(0, file.Name.Length - file.Extension.Length);
            GuiOnly = false;
            return true;
        }

        /// <inheritdoc/>
        public override Command CreateCommand() => ExternalDependencies
            ? new Command
            {
                Name = CommandName,
                Bindings = {new EnvironmentBinding {Name = "CLASSPATH", Insert = RelativePath}},
                Runner = new Runner
                {
                    InterfaceUri = new FeedUri("http://repo.roscidus.com/java/jar-launcher"),
                    Command = NeedsTerminal ? Command.NameRun : Command.NameRunGui,
                    Versions = MinimumRuntimeVersion
                }
            }
            : new Command
            {
                Name = CommandName,
                Path = RelativePath,
                Runner = new Runner
                {
                    InterfaceUri = new FeedUri("http://repo.roscidus.com/java/openjdk-jre"),
                    Command = NeedsTerminal ? Command.NameRun : Command.NameRunGui,
                    Arguments = {@"-jar"},
                    Versions = MinimumRuntimeVersion
                }
            };
    }
}
