// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Basic;

partial class CatalogMan
{
    private class Reset : CatalogSubCommand
    {
        public const string Name = "reset";
        public override string Description => Resources.DescriptionCatalogReset;
        public override string Usage => "";
        protected override int AdditionalArgsMax => 0;

        public Reset(ICommandHandler handler)
            : base(handler)
        {}

        public override ExitCode Execute()
        {
            Services.Feeds.CatalogManager.SetSources(new[] {Services.Feeds.CatalogManager.DefaultSource});
            CatalogManager.GetOnlineSafe();
            return ExitCode.OK;
        }
    }
}
