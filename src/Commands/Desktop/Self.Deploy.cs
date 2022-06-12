// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using ZeroInstall.Commands.Desktop.SelfManagement;

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
            if (_machineWide && WindowsUtils.IsWindows && !WindowsUtils.IsAdministrator)
                throw new NotAdminException(Resources.MustBeAdminForMachineWide);
            if (!_portable && !_library)
            {
                string? existing = FindExistingInstance(_machineWide)
                                ?? FindExistingInstance(machineWide: true);
                if (existing != null && existing != targetDir)
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
            if (AdditionalArgs.Count == 0)
            {
                if (_portable) throw new OptionException(string.Format(Resources.DeployMissingTargetForPortable, "--portable"), "portable");
                return FindExistingInstance(_machineWide)
                    ?? GetDefaultTargetDir();
            }
            else return GetCustomTargetDir();
        }

        private string GetDefaultTargetDir()
        {
            if (WindowsUtils.IsWindows)
            {
                string programFiles = _machineWide ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Programs");
                return Path.Combine(programFiles, "Zero Install");
            }
            else if (UnixUtils.IsMacOSX)
            {
                string applications = _machineWide ? "/Applications" : Path.Combine(Locations.HomeDir, "Applications");
                return Path.Combine(applications, "Zero Install");
            }
            else if (UnixUtils.IsUnix)
                return _machineWide ? "/usr/share/zero-install" : Path.Combine(Locations.HomeDir, ".zero-install");
            else throw new PlatformNotSupportedException();
        }

        private string GetCustomTargetDir()
        {
            string targetDir = Path.GetFullPath(AdditionalArgs[0]);

            if (File.Exists(Path.Combine(targetDir, Locations.PortableFlagName)))
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
                if (FindExistingInstance(machineWide: true) == targetDir)
                {
                    Log.Info($"Detected that '{targetDir}' is an existing machine-wide instance of Zero Install.");
                    _machineWide = true;
                }
                else if (!targetDir.StartsWith(Locations.HomeDir))
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
            Log.Info($"Deploying Zero Install from '{Locations.InstallBase}' to '{targetDir}'");
            manager.Deploy(_library);
        }

        private void RestartCentral(string targetDir)
        {
            if (!WindowsUtils.IsWindows) return;

            var startInfo = new ProcessStartInfo(Path.Combine(targetDir, "ZeroInstall.exe"));
            if (!File.Exists(startInfo.FileName)) return;

            if (_machineWide && WindowsUtils.HasUac)
                startInfo = new("explorer.exe", startInfo.FileName.EscapeArgument()) {UseShellExecute = false};

            startInfo.Start();
        }
    }
}
