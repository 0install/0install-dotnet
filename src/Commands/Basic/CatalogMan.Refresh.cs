// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Commands.Properties;

namespace ZeroInstall.Commands.Basic
{
    partial class CatalogMan
    {
        private class Refresh : CatalogSubCommand
        {
            #region Metadata
            public const string Name = "refresh";

            public override string Description => Resources.DescriptionCatalogRefresh;

            public override string Usage => "";

            protected override int AdditionalArgsMax => 0;

            public Refresh(ICommandHandler handler)
                : base(handler)
            {}
            #endregion

            public override ExitCode Execute()
            {
                CatalogManager.GetOnline();
                return ExitCode.OK;
            }
        }
    }
}
