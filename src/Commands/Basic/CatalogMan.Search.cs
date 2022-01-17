// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common;
using ZeroInstall.Commands.Properties;

namespace ZeroInstall.Commands.Basic;

partial class CatalogMan
{
    private class Search : CatalogSubCommand
    {
        public const string Name = "search";
        public override string Description => Resources.DescriptionCatalogSearch;
        public override string Usage => "[QUERY]";

        public Search(ICommandHandler handler)
            : base(handler)
        {}

        public override ExitCode Execute()
        {
            var catalog = GetCatalog();
            string query = AdditionalArgs.JoinEscapeArguments();

            Handler.Output(Resources.AppList, catalog.Search(query));
            return ExitCode.OK;
        }
    }
}
