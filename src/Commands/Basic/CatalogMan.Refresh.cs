// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Commands.Basic;

partial class CatalogMan
{
    private class Refresh(ICommandHandler handler) : CatalogSubCommand(handler)
    {
        public const string Name = "refresh";
        public override string Description => Resources.DescriptionCatalogRefresh;
        public override string Usage => "";
        protected override int AdditionalArgsMax => 0;

        public override ExitCode Execute()
        {
            CatalogManager.GetOnline();
            return ExitCode.OK;
        }
    }
}
