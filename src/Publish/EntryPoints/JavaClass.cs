// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.EntryPoints
{
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

            Name = file.Name.Substring(0, file.Name.Length - file.Extension.Length);
            GuiOnly = false;
            return true;
        }

        /// <inheritdoc/>
        public override Command CreateCommand() => new Command
        {
            Name = CommandName,
            Path = RelativePath,
            Runner = new Runner
            {
                InterfaceUri = new FeedUri("https://apps.0install.net/java/jre.xml"),
                Command = NeedsTerminal ? Command.NameRun : Command.NameRunGui,
                Versions = new Constraint {NotBefore = MinimumRuntimeVersion}
            }
        };
    }
}
