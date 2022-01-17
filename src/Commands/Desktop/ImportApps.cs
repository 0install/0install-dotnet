// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Imports a set of applications and desktop integrations from an existing <see cref="AppList"/> file.
/// </summary>
public class ImportApps : IntegrationCommand
{
    public const string Name = "import-apps";
    public override string Description => Resources.DescriptionImportApps;
    public override string Usage => "APP-LIST-FILE [OPTIONS]";
    protected override int AdditionalArgsMin => 1;
    protected override int AdditionalArgsMax => 1;

    /// <inheritdoc/>
    public ImportApps(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("no-download", () => Resources.OptionNoDownload, _ => NoDownload = true);
    }

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        var importList = XmlStorage.LoadXml<AppList>(AdditionalArgs[0]);

        using var integrationManager = new IntegrationManager(Config, Handler, MachineWide);
        foreach (var importEntry in importList.Entries)
        {
            var interfaceUri = importEntry.InterfaceUri;
            var appEntry = GetAppEntry(integrationManager, ref interfaceUri);

            if (importEntry.AccessPoints != null)
            {
                var feed = FeedManager[interfaceUri];
                integrationManager.AddAccessPoints(appEntry, feed, importEntry.AccessPoints.Entries);
            }
        }

        return ExitCode.OK;
    }
}
