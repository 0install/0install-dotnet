// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Commands.Basic.Exporters;

namespace ZeroInstall.Commands.Basic
{
    /// <summary>
    /// Exports all <see cref="CliCommand"/> help texts as HTML.
    /// </summary>
    public class ExportHelp : CliCommand
    {
        public const string Name = "export-help";
        public override string Description => "Exports all command help texts as HTML.";
        public override string Usage => "";
        protected override int AdditionalArgsMax => 0;

        /// <inheritdoc/>
        public ExportHelp(ICommandHandler handler)
            : base(handler)
        {}

        public override ExitCode Execute()
        {
            Handler.Output("Zero Install HTML Help Export", new HtmlHelpExporter().ToString());
            return ExitCode.OK;
        }
    }
}
