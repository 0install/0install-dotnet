// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store;
using ZeroInstall.Store.Deployment;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Represents a specific Zero Install instance that is to be deployed, updated or removed.
/// </summary>
/// <remarks>
/// To prevent race-conditions there may only be one maintenance class instance active at any given time.
/// This class acquires a mutex upon calling its constructor and releases it upon calling <see cref="IDisposable.Dispose"/>.
/// </remarks>
public partial class SelfManager : ManagerBase
{
    /// <summary>
    /// The name of the cross-process mutex used to signal that a maintenance operation is currently in progress.
    /// </summary>
    protected override string MutexName => "ZeroInstall.Commands.MaintenanceManager";

    /// <summary>
    /// The full path to the directory containing the Zero Install instance.
    /// </summary>
    public string TargetDir { get; }

    /// <summary>
    /// Controls whether the Zero Install instance at <see cref="TargetDir"/> should be a portable instance.
    /// </summary>
    public bool Portable { get; }

    /// <summary>
    /// Creates a new maintenance manager.
    /// </summary>
    /// <param name="targetDir">The full path to the directory containing the Zero Install instance.</param>
    /// <param name="handler">A callback object used when the user needs to be asked questions or informed about download and IO tasks.</param>
    /// <param name="machineWide">Apply operations machine-wide instead of just for the current user.</param>
    /// <param name="portable">Controls whether the Zero Install instance at <paramref name="targetDir"/> should be a portable instance.</param>
    public SelfManager(string targetDir, ITaskHandler handler, bool machineWide = false, bool portable = false)
        : base(handler, machineWide)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(targetDir)) throw new ArgumentNullException(nameof(targetDir));
        #endregion

        if (portable && machineWide) throw new ArgumentException(string.Format(Resources.ExclusiveOptions, "--portable", "--machine"), nameof(machineWide));

        TargetDir = targetDir;
        Portable = portable;

        try
        {
            AcquireMutex();
        }
        catch (TimeoutException)
        {
            throw new UnauthorizedAccessException("You can only perform one maintenance operation at a time.");
        }
    }

    /// <summary>
    /// Runs the deployment process.
    /// </summary>
    /// <param name="libraryMode">Deploy Zero Install as a library for use by other applications.</param>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    public void Deploy(bool libraryMode = false)
    {
        if (Portable && libraryMode) throw new ArgumentException(string.Format(Resources.ExclusiveOptions, "--portable", "--library"), nameof(libraryMode));

        var newManifest = LoadManifest(Locations.InstallBase);
        var oldManifest = LoadManifest(TargetDir);

        try
        {
            MutexAcquire();

            if (!FileUtils.PathEquals(TargetDir, Locations.InstallBase))
            {
                if (MachineWide)
                    ServiceStop();

                using var clearDir = new ClearDirectory(TargetDir, oldManifest, Handler);
                using var deployDir = new DeployDirectory(Locations.InstallBase, newManifest, TargetDir, Handler);
                deployDir.Stage();
                clearDir.Stage();
                if (Portable) FileUtils.Touch(Path.Combine(TargetDir, Locations.PortableFlagName));
                deployDir.Commit();
                clearDir.Commit();
            }

            if (!Portable)
            {
                DesktopIntegrationApply(newManifest.TotalSize, libraryMode);
                ZeroInstallDeployment.Register(TargetDir, MachineWide, libraryMode);
                RemoveOneGetBootstrap();
            }

            MutexRelease();

            if (MachineWide)
            {
                FirewallRulesApply();
                NgenApply();
                if (ServiceInstall())
                    ServiceStart();
                TaskSchedulerApply(libraryMode);
            }
        }
        catch
        {
            MutexRelease();
            throw;
        }
    }

    /// <summary>
    /// Runs the removal process.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    public void Remove()
    {
        var targetManifest = LoadManifest(TargetDir);

        if (MachineWide) ServiceStop();

        try
        {
            MutexAcquire();

            if (MachineWide)
            {
                TaskSchedulerRemove();
                ServiceUninstall();
                NgenRemove();
                FirewallRulesRemove();
            }

            using (var clearDir = new ClearDirectory(TargetDir, targetManifest, Handler) {NoRestart = true})
            {
                clearDir.Stage();
                DeleteServiceLogFiles();
                if (Portable) File.Delete(Path.Combine(TargetDir, Locations.PortableFlagName));
                clearDir.Commit();
            }

            if (!Portable)
            {
                DesktopIntegrationRemove();
                ZeroInstallDeployment.Unregister(MachineWide);
            }
        }
        finally
        {
           MutexRelease();
        }
    }

#if NETFRAMEWORK
    /// <summary>
    /// Runs a command-line without a visible window.
    /// </summary>
    private static void RunHidden(string fileName, params string[] arguments)
    => new System.Diagnostics.ProcessStartInfo(fileName, arguments.JoinEscapeArguments())
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetTempPath() // Avoid locking the current directory
        }
       .Run();
#endif
}
