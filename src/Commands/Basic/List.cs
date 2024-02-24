// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// List all known interface (program) URIs.
/// </summary>
/// <remarks>If a search term is given, only URIs containing that string are shown (case insensitive).</remarks>
public class List(ICommandHandler handler) : CliCommand(handler)
{
    public const string Name = "list";
    public override string Description => Resources.DescriptionList;
    public override string Usage => "[PATTERN]";
    protected override int AdditionalArgsMax => 1;

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        var feeds = new CacheNodeBuilder(Handler, FeedCache).Build().OfType<FeedNode>();
        if (AdditionalArgs is [var pattern])
            feeds = feeds.Where(x => x.Uri.ToStringRfc().ContainsIgnoreCase(pattern) || x.Name.Contains(pattern));

        Handler.Output(Resources.FeedsCached, feeds);
        return ExitCode.OK;
    }
}
