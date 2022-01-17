// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// List all current <see cref="AppEntry"/>s in the <see cref="AppList"/>.
/// </summary>
public class ListApps : IntegrationCommand
{
    public const string Name = "list-apps";
    public override string Description => Resources.DescriptionListApps;
    public override string Usage => "[URI|PATTERN]";
    protected override int AdditionalArgsMax => 1;

    /// <summary>Indicates the user wants a machine-readable output.</summary>
    private bool _xmlOutput;

    /// <inheritdoc/>
    public ListApps(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("xml", () => Resources.OptionXml, _ => _xmlOutput = true);
    }

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        var apps = AppList.LoadSafe(MachineWide);

        if (AdditionalArgs.Count > 0)
        {
            if (Uri.TryCreate(AdditionalArgs[0], UriKind.Absolute, out var uri))
            {
                var feedUri = new FeedUri(uri);
                apps.Entries.RemoveAll(x => x.InterfaceUri != feedUri);
            }
            else
                apps.Entries.RemoveAll(x => !x.Name.ContainsIgnoreCase(AdditionalArgs[0]));
        }

        if (_xmlOutput) Handler.Output(Resources.MyApps, apps.ToXmlString());
        else Handler.Output(Resources.MyApps, apps.Entries);
        return ExitCode.OK;
    }
}
