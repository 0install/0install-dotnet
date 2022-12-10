// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;

namespace ZeroInstall.Commands.Desktop;

partial class Self
{
    public abstract class RemoveSubCommandBase : SelfSubCommand
    {
        protected RemoveSubCommandBase(ICommandHandler handler)
            : base(handler)
        {}

        protected abstract string TargetDir { get; }

        // Auto-detect portable targets by looking for flag file
        protected bool Portable => File.Exists(Path.Combine(TargetDir, Locations.PortableFlagName));

        // Auto-detect machine-wide targets by comparing path with registry entry
        protected bool MachineWide => !Portable && (TargetDir == FindExistingInstance(machineWide: true));

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
    public class Remove : RemoveSubCommandBase
    {
        public const string Name = "remove";
        public override string Description => Resources.DescriptionMaintenanceRemove;
        public override string Usage => "";
        protected override int AdditionalArgsMax => 0;

        public Remove(ICommandHandler handler)
            : base(handler)
        {}

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

            if (!ZeroInstallInstance.IsLibraryMode && !Handler.Ask(Resources.AskRemoveZeroInstall, defaultAnswer: true))
                return ExitCode.UserCanceled;

            if (IntegrationCommand.ExistingDesktopIntegration())
                new RemoveAllApps(Handler).Execute();
            if (MachineWide && IntegrationCommand.ExistingDesktopIntegration(machineWide: true))
                new RemoveAllApps(Handler) {MachineWide = true}.Execute();

            if (Handler.Ask(Resources.ConfirmPurge, defaultAnswer: ZeroInstallInstance.IsLibraryMode && ZeroInstallInstance.FindOther() == null))
            {
                try
                {
                    ImplementationStore.Purge();
                }
                catch (NotAdminException ex) when (ZeroInstallInstance.IsLibraryMode)
                {
                    Log.Info("Unable to purge implementation store", ex);
                }
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

            var args = new[] {Self.Name, RemoveHelper.Name, Locations.InstallBase};
            if (Handler.Verbosity == Verbosity.Batch) args = args.Append("--batch");
            if (Handler.Background) args = args.Append("--background");

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
    private class RemoveHelper : RemoveSubCommandBase
    {
        public const string Name = "remove-helper";
        public override string Description => "Internal helper for '0install maintenance remove' used to support self-removal on Windows.";
        public override string Usage => "TARGET";
        protected override int AdditionalArgsMin => 1;
        protected override int AdditionalArgsMax => 1;

        public RemoveHelper(ICommandHandler handler)
            : base(handler)
        {}

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
