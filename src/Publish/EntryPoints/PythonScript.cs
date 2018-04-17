// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using System.IO;
using JetBrains.Annotations;
using NanoByte.Common;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// A script written in Python.
    /// </summary>
    public sealed class PythonScript : InterpretedScript
    {
        /// <summary>
        /// Does this application have a graphical interface an no terminal output? Only enable if you are sure!
        /// </summary>
        [Category("Details (Python)"), DisplayName(@"GUI only"), Description("Does this application have a graphical interface an no terminal output? Only enable if you are sure!")]
        [UsedImplicitly]
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
        protected override FeedUri InterpreterInterface => new FeedUri("http://repo.roscidus.com/python/python");

        /// <inheritdoc/>
        public override Command CreateCommand() => new Command
        {
            Name = CommandName,
            Path = RelativePath,
            Runner = new Runner
            {
                InterfaceUri = InterpreterInterface,
                Versions = InterpreterVersions,
                Command = NeedsTerminal ? Command.NameRun : Command.NameRunGui
            }
        };
    }
}
