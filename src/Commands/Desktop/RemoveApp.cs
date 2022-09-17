// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.DesktopIntegration;
using ZeroInstall.Model.Capabilities;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store.Configuration;

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

        if (appEntry.AccessPoints != null)
            CheckInstallBase();

        foreach (var hook in appEntry.CapabilityLists.CompatibleCapabilities().OfType<RemoveHook>())
            RunRemoveHook(hook);

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
        var selections = SolveOffline(hook.Command);
        if (selections == null)
        {
            Log.Info($"Skipped remove hook {hook.ID} for {InterfaceUri} because the app is not cached");
            return;
        }

        Executor.Inject(selections)
                .AddArguments(hook.Arguments.Select(x => x.Value).ToArray())
                .Start()
               ?.WaitForExitCode(); // Log the exit code but continue regardless of its value
    }

    /// <summary>
    /// Trys to generate <see cref="Selections"/> for running the specified <paramref name="command"/> without downloading anything.
    /// </summary>
    private Selections? SolveOffline(string? command)
    {
        Config.NetworkUse = NetworkLevel.Offline;
        try
        {
            var selections = Solver.Solve(new(InterfaceUri, command));
            return SelectionsManager.GetUncachedImplementations(selections).Any() ? null : selections;
        }
        catch (Exception ex) when (ex is SolverException or WebException)
        {
            Log.Debug("Failed to solve dependencies without downloading anything", ex);
            return null;
        }
    }
}
