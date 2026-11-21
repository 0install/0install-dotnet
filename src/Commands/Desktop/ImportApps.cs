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

        var exitCode = ExitCode.OK;
        using var integrationManager = new IntegrationManager(Config, Handler, MachineWide);
        foreach (var importEntry in importList.Entries)
        {
            try
            {
                var interfaceUri = importEntry.InterfaceUri;
                var appEntry = interfaceUri.IsPetName
                    ? GetOrAddNamedApp(integrationManager, importEntry)
                    : GetAppEntry(integrationManager, ref interfaceUri);

                if (importEntry.AccessPoints != null)
                {
                    var feed = FeedManager[appEntry.EffectiveRequirements.InterfaceUri];
                    integrationManager.AddAccessPoints(appEntry, feed, importEntry.AccessPoints.Entries);
                }
            }
            catch (ConflictException ex)
            {
                // Continue importing the remaining entries
                Log.Warn(ex);
                exitCode = ExitCode.Conflict;
            }
        }

        return exitCode;
    }

    /// <summary>
    /// Gets an existing named app matching the pet-name of <paramref name="importEntry"/> or adds a new one.
    /// Updates the <see cref="AppEntry.Requirements"/> of an existing named app if they differ from the imported ones.
    /// </summary>
    private AppEntry GetOrAddNamedApp(IIntegrationManager integrationManager, AppEntry importEntry)
    {
        if (importEntry.Requirements is not {} requirements)
            throw new InvalidDataException(string.Format(Resources.NamedAppMissingRequirements, importEntry.PetName));

        EnsureAllowed(requirements.InterfaceUri);
        var feed = FeedManager[requirements.InterfaceUri];

        if (integrationManager.AppList.GetEntry(importEntry.InterfaceUri) is {} existingEntry)
        {
            if (!requirements.Equals(existingEntry.Requirements))
                integrationManager.UpdateApp(existingEntry, feed, requirements);
            return existingEntry;
        }

        return integrationManager.AddApp(importEntry.PetName!, requirements, feed);
    }
}
