// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common.Tasks;
using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration.Windows;

namespace ZeroInstall.Commands.Desktop.SelfManagement
{
    partial class SelfManager
    {
        /// <summary>
        /// Adds Zero Install to the start menu and the PATH environment variable.
        /// </summary>
        private void DesktopIntegrationApply()
        {
            Handler.RunTask(new SimpleTask(Resources.DesktopIntegrationApply, () =>
            {
                Shortcut.Create(
                    path: Shortcut.GetStartMenuPath("", "Zero Install", MachineWide),
                    targetPath: Path.Combine(TargetDir, "ZeroInstall.exe"),
                    appId: "ZeroInstall");

                PathEnv.AddDir(TargetDir, MachineWide);
            }));
        }

        /// <summary>
        /// Removes Zero Install from the start menu and the PATH environment variable.
        /// </summary>
        private void DesktopIntegrationRemove()
        {
            static void DeleteIfExists(string path)
            {
                if (File.Exists(path)) File.Delete(path);
            }

            Handler.RunTask(new SimpleTask(Resources.DesktopIntegrationRemove, () =>
            {
                DeleteIfExists(Shortcut.GetStartMenuPath("", "Zero Install", MachineWide));
                DeleteIfExists(Shortcut.GetStartMenuPath("Zero install", "Zero Install", MachineWide));
                DeleteIfExists(Shortcut.GetDesktopPath("Zero Install", MachineWide));

                PathEnv.RemoveDir(TargetDir, MachineWide);
            }));
        }
    }
}
