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
            Handler.Output(
                Resources.AppList,
                CatalogManager.Get().Search(query: AdditionalArgs.JoinEscapeArguments()));
            return ExitCode.OK;
        }
    }
}
