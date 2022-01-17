// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Preferences;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Un-register a feed, reversing the effect of <see cref="AddFeed"/>.
/// </summary>
public class RemoveFeed : AddRemoveFeedCommand
{
    public const string Name = "remove-feed";
    public override string Description => Resources.DescriptionRemoveFeed;

    /// <inheritdoc/>
    public RemoveFeed(ICommandHandler handler)
        : base(handler)
    {}

    /// <inheritdoc/>
    protected override ExitCode ExecuteHelper(IEnumerable<FeedUri> interfaces, FeedReference source, Stability suggestedStabilityPolicy)
    {
        #region Sanity checks
        if (interfaces == null) throw new ArgumentNullException(nameof(interfaces));
        if (source == null) throw new ArgumentNullException(nameof(source));
        #endregion

        var modifiedInterfaces = new List<FeedUri>();
        foreach (var interfaceUri in interfaces)
        {
            var preferences = InterfacePreferences.LoadFor(interfaceUri);
            if (preferences.Feeds.Remove(source))
            {
                modifiedInterfaces.Add(interfaceUri);
                if (preferences.StabilityPolicy == suggestedStabilityPolicy && suggestedStabilityPolicy != Stability.Unset)
                {
                    if (Handler.Ask(string.Format(Resources.StabilityPolicyReset, interfaceUri.ToStringRfc()), defaultAnswer: false))
                        preferences.StabilityPolicy = Stability.Unset;
                }
                preferences.SaveFor(interfaceUri);
            }
        }

        if (modifiedInterfaces.Count == 0)
        {
            Handler.OutputLow(Resources.FeedManagement, Resources.FeedNotRegistered);
            return ExitCode.NoChanges;
        }
        else
        {
            Handler.OutputLow(Resources.FeedManagement,
                Resources.FeedUnregistered + Environment.NewLine +
                StringUtils.Join(Environment.NewLine, modifiedInterfaces.Select(x => x.ToStringRfc())));
            return ExitCode.OK;
        }
    }
}
