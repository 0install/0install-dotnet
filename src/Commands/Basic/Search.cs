// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Feeds;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Searches for feeds indexed by the mirror server.
/// </summary>
public class Search(ICommandHandler handler) : CliCommand(handler)
{
    public const string Name = "search";
    public override string Description => Resources.DescriptionSearch;
    public override string Usage => "QUERY";
    protected override int AdditionalArgsMin => (Handler is CliCommandHandler) ? 1 : 0;

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        string keywords = string.Join(" ", AdditionalArgs);
        Handler.Output(keywords, SearchResults.Query(Config, keywords));
        return ExitCode.OK;
    }
}
