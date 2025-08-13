// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using ZeroInstall.DesktopIntegration;

namespace ZeroInstall.Commands.Desktop;

partial class Self
{
    public abstract class RemoveSubCommandBase(ICommandHandler handler) : SelfSubCommand(handler)
    {
        protected abstract string TargetDir { get; }

        // Auto-detect portable targets by looking for flag file
        protected bool Portable => File.Exists(Path.Combine(TargetDir, Locations.PortableFlagName));

        // Auto-detect machine-wide targets by comparing path with registry entry
        protected bool MachineWide => !Portable && TargetDir == ZeroInstallDeployment.GetPath(machineWide: true);

        protected void PerformRemove()
        {
            using var manager = new SelfManager(TargetDir, Handler, MachineWide, Portable);
            Log.Info($"Using Zero Install instance at '{Locations.InstallBase}' to remove '{TargetDir}'");
            manager.Remove();
        }
    }

    /// <summary>
    /// Removes the current instance of Zero Install from the system.
    /// </summary>
    public class Remove(ICommandHandler handler) : RemoveSubCommandBase(handler)
    {
        public const string Name = "remove";
        public override string Description => Resources.DescriptionMaintenanceRemove;
        public override string Usage => "";
        protected override int AdditionalArgsMax => 0;

        protected override string TargetDir => Locations.InstallBase;

        public override ExitCode Execute()
        {
            if (!ZeroInstallInstance.IsDeployed)
            {
                Log.Error(Resources.SelfRemoveNotDeployed);
                return ExitCode.NoChanges;
            }

            if (MachineWide && WindowsUtils.IsWindows && !WindowsUtils.IsAdministrator)
                throw new NotAdminException(Resources.MustBeAdminForMachineWide);

            if (!Handler.Ask(Resources.AskRemoveZeroInstall, defaultAnswer: true))
                return ExitCode.UserCanceled;

            if (!AppList.IsEmpty())
                new RemoveAllApps(Handler).Execute();
            if (MachineWide && !AppList.IsEmpty(machineWide: true))
                new RemoveAllApps(Handler) {MachineWide = true}.Execute();

            bool autoPurgeStore = ZeroInstallInstance.IsLibraryMode && ZeroInstallDeployment.FindOther() == null;
            if (Handler.Ask(Resources.ConfirmPurge, defaultAnswer: autoPurgeStore))
            {
                try
                {
                    ImplementationStore.Purge();
                }
                #region Error handling
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && ZeroInstallInstance.IsLibraryMode)
                {
                    Log.Warn("Unable to purge implementation store", ex);
                }
                #endregion
            }

            if (WindowsUtils.IsWindows) DelegateToTempCopy();
            else PerformRemove();

            return ExitCode.OK;
        }

        /// <summary>
        /// Deploys a portable copy of Zero Install to a temp directory and delegates the actual removal of the current instance to this copy.
        /// </summary>
        private void DelegateToTempCopy()
        {
            string tempDir = new TemporaryDirectory("0install-remove");
            using (var manager = new SelfManager(tempDir, Handler, machineWide: false, portable: true))
                manager.Deploy();

            string path = Path.Combine(tempDir, "0install-win.exe");
            if (!File.Exists(path)) path =  Path.Combine(tempDir, "0install.exe");

            var args = new List<string> {Self.Name, RemoveHelper.Name, Locations.InstallBase};
            if (Handler.Verbosity == Verbosity.Batch) args.Add("--batch");
            if (Handler.Background) args.Add("--background");

            new ProcessStartInfo(path, args.JoinEscapeArguments())
            {
                UseShellExecute = false,
                WorkingDirectory = tempDir
            }.Start();
        }
    }

    /// <summary>
    /// Internal helper for <see cref="Remove"/> used to support self-removal on Windows.
    /// </summary>
    private class RemoveHelper(ICommandHandler handler) : RemoveSubCommandBase(handler)
    {
        public const string Name = "remove-helper";
        public override string Description => "Internal helper for '0install maintenance remove' used to support self-removal on Windows.";
        public override string Usage => "TARGET";
        protected override int AdditionalArgsMin => 1;
        protected override int AdditionalArgsMax => 1;

        protected override string TargetDir => AdditionalArgs[0];

        public override ExitCode Execute()
        {
            try
            {
                if (!Locations.IsPortable || !WindowsUtils.IsWindows)
                    throw new NotSupportedException("This command is used as an internal helper and should not be called manually.");

                PerformRemove();

                return ExitCode.OK;
            }
            finally
            {
                WindowsSelfDelete();
            }
        }
    }

    /// <summary>
    /// Use cmd.exe to delete own installation directory after 8s delay
    /// </summary>
    private static void WindowsSelfDelete()
        => new ProcessStartInfo("cmd.exe", $"/c (ping 127.0.0.1 -n 8 || ping ::1 -n 8) & rd /s /q {Locations.InstallBase.EscapeArgument()}")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        }.Start();
}
