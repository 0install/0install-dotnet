// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Remove an application from the <see cref="AppList"/> and undoes any desktop environment integration.
/// </summary>
public class RemoveApp(ICommandHandler handler) : AppCommand(handler)
{
    public const string Name = "remove";
    public const string AltName = "remove-app";
    public const string AltName2 = "destory";
    public override string Description => Resources.DescriptionRemoveApp;
    public override string Usage => "[OPTIONS] (ALIAS|INTERFACE)";

    /// <inheritdoc/>
    protected override ExitCode ExecuteHelper()
    {
        if (IntegrationManager.AppList.GetEntry(InterfaceUri) is not {} appEntry)
        {
            Log.Warn(string.Format(Resources.AliasNotFound, InterfaceUri));
            return ExitCode.NoChanges;
        }

        if (appEntry.AccessPoints != null)
            CheckInstallBase();

        foreach (var hook in appEntry.CapabilityLists.CompatibleCapabilities().OfType<RemoveHook>())
            RunRemoveHook(hook);

        IntegrationManager.RemoveApp(appEntry);

        if (ZeroInstallInstance.IsLibraryMode
         && AppList.IsEmpty()
         && (!ZeroInstallInstance.IsMachineWide || AppList.IsEmpty(machineWide: true)))
        {
            Log.Info("Last app removed, auto-removing library mode Zero Install instance");
            StartCommandBackground(Self.Name, Self.Remove.Name, "--batch");
        }

        return ExitCode.OK;
    }

    /// <summary>
    /// Runs a remove <paramref name="hook"/> if the app is already cached.
    /// </summary>
    private void RunRemoveHook(RemoveHook hook)
    {
        if (Handler.Verbosity == Verbosity.Batch)
        {
            Log.Info($"Skipped remove hook {hook.ID} for {InterfaceUri} because running in batch mode");
            return;
        }

        Log.Debug($"Solving remove hook {hook.ID} for {InterfaceUri}");
        if (TrySolveOffline(new(InterfaceUri, hook.Command)) is {} selections)
        {
            Executor.Inject(selections)
                    .AddArguments(hook.Arguments.Select(x => x.Value).ToArray())
                    .Start()
                   ?.WaitForExitCode(); // Log the exit code but continue regardless of its value
        }
        else Log.Info($"Skipped remove hook {hook.ID} for {InterfaceUri} because the app is not cached");
    }
}
