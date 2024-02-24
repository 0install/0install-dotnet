// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Commands.Basic;

partial class CatalogMan
{
    private class List(ICommandHandler handler) : CatalogSubCommand(handler)
    {
        public const string Name = "list";
        public override string Description => Resources.DescriptionCatalogList;
        public override string Usage => "";
        protected override int AdditionalArgsMax => 0;

        public override ExitCode Execute()
        {
            Handler.Output(Resources.CatalogSources, CatalogManager.GetSources());
            return ExitCode.OK;
        }
    }
}
