// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Basic;

partial class CatalogMan
{
    private class Reset(ICommandHandler handler) : CatalogSubCommand(handler)
    {
        public const string Name = "reset";
        public override string Description => Resources.DescriptionCatalogReset;
        public override string Usage => "";
        protected override int AdditionalArgsMax => 0;

        public override ExitCode Execute()
        {
            Services.Feeds.CatalogManager.SetSources([Services.Feeds.CatalogManager.DefaultSource]);
            CatalogManager.GetOnlineSafe();
            return ExitCode.OK;
        }
    }
}
