// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Basic;

partial class CatalogMan
{
    private class Search(ICommandHandler handler) : CatalogSubCommand(handler)
    {
        public const string Name = "search";
        public override string Description => Resources.DescriptionCatalogSearch;
        public override string Usage => "[QUERY]";

        public override ExitCode Execute()
        {
            var catalog = CatalogManager.GetCached() ?? CatalogManager.GetOnlineSafe();
            string query = AdditionalArgs.JoinEscapeArguments();

            Handler.Output(Resources.AppList, catalog.Search(query));
            return ExitCode.OK;
        }
    }
}
