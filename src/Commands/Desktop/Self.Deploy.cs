// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;

namespace ZeroInstall.Commands.Desktop;

partial class Self
{
    /// <summary>
    /// Deploys Zero Install to a target directory and integrates it in the system.
    /// </summary>
    public class Deploy : SelfSubCommand
    {
        public const string Name = "deploy";
        public override string Description => Resources.DescriptionMaintenanceDeploy;
        public override string Usage => "[TARGET]";
        protected override int AdditionalArgsMax => 1;

        /// <summary>Apply operations machine-wide instead of just for the current user.</summary>
        private bool _machineWide;

        /// <summary>Create a portable installation.</summary>
        private bool _portable;

        /// <summary>Deploy Zero Install as a library for use by other applications without its own desktop integration.</summary>
        private bool _library;

        /// <summary>Indicates whether the installer shall restart the <see cref="Central"/> GUI after the installation.</summary>
        private bool _restartCentral;

        public Deploy(ICommandHandler handler)
            : base(handler)
        {
            Options.Add("m|machine", () => Resources.OptionMachine, _ => _machineWide = true);
            Options.Add("p|portable", () => Resources.OptionPortable, _ => _portable = true);
            Options.Add("l|library", () => Resources.OptionLibrary, _ => _library = true);
            Options.Add("restart-central", () => Resources.OptionRestartCentral, _ => _restartCentral = true);
        }

        public override ExitCode Execute()
        {
            string targetDir = GetTargetDir();
            if (_machineWide && !OSUtils.IsAdmin)
                throw new NotAdminException(Resources.MustBeAdminForMachineWide);
            if (!_portable && !_library)
            {
                if (ZeroInstallInstance.FindOtherDeployment(_machineWide) is {} existing && existing != targetDir)
                {
                    string hint = string.Format(Resources.ExistingInstance, existing);
                    if (!Handler.Ask(string.Format(Resources.AskDeployNewTarget, targetDir) + Environment.NewLine + hint, defaultAnswer: true, alternateMessage: hint))
                        return ExitCode.UserCanceled;
                }
                else if (!Handler.Ask(Resources.AskDeployZeroInstall, defaultAnswer: true))
                    return ExitCode.UserCanceled;
            }

            bool newDirectory = !Directory.Exists(targetDir);
            PerformDeploy(targetDir);

            if (_restartCentral)
                RestartCentral(targetDir);
            else if (_portable)
                Handler.OutputLow(Resources.PortableMode, string.Format(Resources.DeployedPortable, targetDir));
            else if (newDirectory && !_library && !Handler.IsGui)
                Handler.OutputLow(Resources.Added0installToPath, Resources.ReopenTerminal);

            return ExitCode.OK;
        }

        private string GetTargetDir()
        {
            if (AdditionalArgs is [])
            {
                if (_portable) throw new OptionException(string.Format(Resources.DeployMissingTargetForPortable, "--portable"), "portable");
                if (!_machineWide && IsUnregisteredInMachineWideDefault)
                {
                    Log.Info($"Detected that '{ZeroInstallInstance.DeploymentRoot}' is an unregistered instance of Zero Install in the default machine-wide location.");
                    _machineWide = true;
                }
                return ZeroInstallDeployment.GetPath(_machineWide)
                    ?? GetDefaultTargetDir(_machineWide);
            }
            else return GetCustomTargetDir();
        }

        private static string GetDefaultTargetDir(bool machineWide)
            => ZeroInstallDeployment.GetDefaultPath(machineWide, appBundle: ZeroInstallInstance.IsAppBundle);

        /// <summary>
        /// Indicates whether the current instance is located in the default machine-wide location without being registered,
        /// e.g., because the user copied an app bundle to <c>/Applications</c> manually.
        /// Deploying such an instance machine-wide applies desktop integration without copying any files.
        /// </summary>
        private static bool IsUnregisteredInMachineWideDefault
            => UnixUtils.IsUnix
            && !Locations.IsPortable
            && ZeroInstallDeployment.GetPath(machineWide: false) == null
            && ZeroInstallDeployment.GetPath(machineWide: true) == null
            && Paths.AreEqual(ZeroInstallInstance.DeploymentRoot, GetDefaultTargetDir(machineWide: true));

        private string GetCustomTargetDir()
        {
            string targetDir = Paths.Absolute(AdditionalArgs[0]);

            if (ZeroInstallDeployment.IsPortable(targetDir))
            {
                Log.Info($"Detected that '{targetDir}' is an existing portable instance of Zero Install.");
                _portable = true;
            }

            if (_portable)
            {
                if (_machineWide)
                    throw new OptionException(string.Format(Resources.ExclusiveOptions, "--portable", "--machine"), "machine");
            }
            else if (!_machineWide)
            {
                if (ZeroInstallDeployment.GetPath(machineWide: true) == targetDir)
                {
                    Log.Info($"Detected that '{targetDir}' is an existing machine-wide instance of Zero Install.");
                    _machineWide = true;
                }
                else if (!targetDir.StartsWithOrdinal(Locations.HomeDir))
                {
                    string hint = string.Format(Resources.DeployTargetOutsideHome, targetDir);
                    if (Handler.Ask(Resources.AskDeployMachineWide + Environment.NewLine + hint, defaultAnswer: false, alternateMessage: hint))
                        _machineWide = true;
                }
            }
            return targetDir;
        }

        private void PerformDeploy(string targetDir)
        {
            using var manager = new SelfManager(targetDir, Handler, _machineWide, _portable);
            Log.Info($"Deploying Zero Install from '{ZeroInstallInstance.DeploymentRoot}' to '{targetDir}'");
            manager.Deploy(_library);
        }

        private void RestartCentral(string targetDir)
        {
            if (!WindowsUtils.IsWindows) return;

            var startInfo = new ProcessStartInfo(Paths.Combine(targetDir, "ZeroInstall.exe"));
            if (!File.Exists(startInfo.FileName)) return;

            if (_machineWide && WindowsUtils.HasUac)
            {
                // Use indirection via explorer.exe to de-elevate
                startInfo = new("explorer.exe", startInfo.FileName.EscapeArgument()) {UseShellExecute = false};
            }

            startInfo.Start();
        }
    }
}
