// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Remove an application from the <see cref="AppList"/> and undoes any desktop environment integration.
/// </summary>
public class RemoveApp : AppCommand
{
    public const string Name = "remove";
    public const string AltName = "remove-app";
    public const string AltName2 = "destory";
    public override string Description => Resources.DescriptionRemoveApp;
    public override string Usage => "[OPTIONS] (ALIAS|INTERFACE)";

    /// <inheritdoc/>
    public RemoveApp(ICommandHandler handler)
        : base(handler)
    {}

    /// <inheritdoc/>
    protected override ExitCode ExecuteHelper()
    {
        var appEntry = IntegrationManager.AppList.GetEntry(InterfaceUri);
        if (appEntry == null)
        {
            Log.Warn(string.Format(Resources.AliasNotFound, InterfaceUri));
            return ExitCode.NoChanges;
        }

        IntegrationManager.RemoveApp(appEntry);

        if (ZeroInstallInstance.IsLibraryMode
         && !ExistingDesktopIntegration()
         && (!ZeroInstallInstance.IsMachineWide || !ExistingDesktopIntegration(machineWide: true)))
        {
            Log.Info("Last app removed, auto-removing library mode Zero Install instance");
            StartCommandBackground(Self.Name, Self.Remove.Name, "--batch");
        }

        return ExitCode.OK;
    }
}
