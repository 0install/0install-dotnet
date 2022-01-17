// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Preferences;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// List all known feed URIs for a specific interface.
/// </summary>
public class ListFeeds : CliCommand
{
    public const string Name = "list-feeds";
    public override string Description => Resources.DescriptionListFeeds;
    public override string Usage => "[OPTIONS] URI";
    protected override int AdditionalArgsMin => 1;
    protected override int AdditionalArgsMax => 1;

    /// <inheritdoc/>
    public ListFeeds(ICommandHandler handler)
        : base(handler)
    {}

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        var interfaceUri = GetCanonicalUri(AdditionalArgs[0]);
        var preferences = InterfacePreferences.LoadFor(interfaceUri);

        Handler.Output(
            string.Format(Resources.FeedsRegistered, interfaceUri),
            preferences.Feeds.Select(x => x.Source));
        return ExitCode.OK;
    }
}
