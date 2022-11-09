// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.ViewModel;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// List all known interface (program) URIs.
/// </summary>
/// <remarks>If a search term is given, only URIs containing that string are shown (case insensitive).</remarks>
public class List : CliCommand
{
    public const string Name = "list";
    public override string Description => Resources.DescriptionList;
    public override string Usage => "[PATTERN]";
    protected override int AdditionalArgsMax => 1;

    /// <inheritdoc/>
    public List(ICommandHandler handler)
        : base(handler)
    {}

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        var feeds = new CacheNodeBuilder(Handler, FeedCache).Build().OfType<FeedNode>();
        if (AdditionalArgs.Count > 0)
            feeds = feeds.Where(x => x.Uri.ToStringRfc().ContainsIgnoreCase(AdditionalArgs[0]) || x.Name.Contains(AdditionalArgs[0]));

        Handler.Output(Resources.FeedsCached, feeds);
        return ExitCode.OK;
    }
}
