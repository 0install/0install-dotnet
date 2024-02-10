// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Preferences;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Common base class for <see cref="AddFeed"/> and <see cref="RemoveFeed"/>.
/// </summary>
public abstract class AddRemoveFeedCommand : CliCommand
{
    public override string Usage => "[OPTIONS] [INTERFACE] FEED";
    protected override int AdditionalArgsMin => 1;
    protected override int AdditionalArgsMax => 2;

    /// <inheritdoc/>
    protected AddRemoveFeedCommand(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("o|offline", () => Resources.OptionOffline, _ => Config.NetworkUse = NetworkLevel.Offline);
        Options.Add("r|refresh", () => Resources.OptionRefresh, _ => FeedManager.Refresh = true);
    }

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        FeedUri feedUri;
        ICollection<FeedUri> interfaces;
        Stability suggestedStabilityPolicy;

        if (AdditionalArgs.Count == 2)
        { // Main interface for feed specified explicitly
            interfaces = [GetCanonicalUri(AdditionalArgs[0])];
            feedUri = GetCanonicalUri(AdditionalArgs[1]);
            suggestedStabilityPolicy = Stability.Unset;
        }
        else
        { // Determine interfaces from feed content (<feed-for> tags)
            feedUri = GetCanonicalUri(AdditionalArgs[0]);

            var feed = FeedManager.GetFresh(feedUri);
            interfaces = feed.FeedFor.Select(reference => reference.Target).WhereNotNull().ToList();
            if (interfaces.Count == 0)
                throw new OptionException(string.Format(Resources.MissingFeedFor, feedUri), null);

            suggestedStabilityPolicy = feed.Implementations.Select(x => x.Stability).DefaultIfEmpty().Max();
        }

        EnsureAllowed(feedUri);
        return ExecuteHelper(interfaces, new FeedReference {Source = feedUri}, suggestedStabilityPolicy);
    }

    /// <summary>
    /// Registers or unregisters an additional feed source for a set of interfaces.
    /// </summary>
    /// <param name="interfaces">The set of interface URIs to register the feed <paramref name="source"/> for.</param>
    /// <param name="source">The feed reference to register for the <paramref name="interfaces"/>.</param>
    /// <param name="suggestedStabilityPolicy">The suggested value for <see cref="InterfacePreferences.StabilityPolicy"/>. Will be <see cref="Stability.Unset"/> unless there is exactly one <see cref="Implementation"/> in the <see cref="Feed"/>.</param>
    protected abstract ExitCode ExecuteHelper(IEnumerable<FeedUri> interfaces, FeedReference source, Stability suggestedStabilityPolicy);
}
