// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store;
using ZeroInstall.Store.Deployment;

namespace ZeroInstall.Commands.Desktop.SelfManagement;

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
    /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
    /// <param name="machineWide">Apply operations machine-wide instead of just for the current user.</param>
    /// <param name="portable">Controls whether the Zero Install instance at <paramref name="targetDir"/> should be a portable instance.</param>
    public SelfManager(string targetDir, ITaskHandler handler, bool machineWide = false, bool portable = false)
        : base(handler, machineWide)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(targetDir)) throw new ArgumentNullException(nameof(targetDir));
        #endregion

        if (portable && machineWide) throw new ArgumentException(string.Format(Resources.CannotUseOptionsTogether, "--portable", "--machine"), nameof(machineWide));

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
    /// <param name="libraryMode">Deploy Zero Install as a library for use by other applications without its own desktop integration.</param>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    public void Deploy(bool libraryMode = false)
    {
        if (Portable && libraryMode) throw new ArgumentException(string.Format(Resources.CannotUseOptionsTogether, "--portable", "--library"), nameof(libraryMode));

        var newManifest = LoadManifest(Locations.InstallBase);
        var oldManifest = LoadManifest(TargetDir);

        if (MachineWide)
            ServiceStop();

        try
        {
            TargetMutexAcquire();

            if (TargetDir != Locations.InstallBase)
            {
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
                if (!libraryMode)
                    DesktopIntegrationApply(newManifest.TotalSize);
                ZeroInstallInstance.RegisterLocation(TargetDir, MachineWide, libraryMode);
                RemoveOneGetBootstrap();
            }

            TargetMutexRelease();

            if (MachineWide)
            {
                NgenApply();
                if (ServiceInstall())
                    ServiceStart();
                TaskSchedulerApply(libraryMode);
            }
        }
        catch
        {
            TargetMutexRelease();
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
            TargetMutexAcquire();

            if (MachineWide)
            {
                TaskSchedulerRemove();
                ServiceUninstall();
                NgenRemove();
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
                ZeroInstallInstance.UnregisterLocation(MachineWide);
            }
        }
        finally
        {
           TargetMutexRelease();
        }
    }
}
