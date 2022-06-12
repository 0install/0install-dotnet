// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using NanoByte.Common.Info;
using NanoByte.Common.Native;
using ZeroInstall.DesktopIntegration.Windows;

namespace ZeroInstall.Commands.Desktop.SelfManagement;

partial class SelfManager
{
    private const string UninstallID = "Zero Install_is1";

    /// <summary>
    /// Adds Zero Install to the start menu and the PATH environment variable.
    /// </summary>
    /// <param name="size">The size of the installed files in bytes.</param>
    private void DesktopIntegrationApply(long size)
    {
        if (WindowsUtils.IsWindows) DesktopIntegrationApplyWindows(size);
    }

    [SupportedOSPlatform("windows")]
    private void DesktopIntegrationApplyWindows(long size)
    {
        Handler.RunTask(new SimpleTask(Resources.DesktopIntegrationApply, () =>
        {
            UninstallEntry.Register(
                UninstallID,
                new[] { Path.Combine(TargetDir, "0install-win.exe"), Self.Name, Self.Remove.Name },
                "Zero Install",
                new("https://0install.net/"),
                iconPath: Path.Combine(TargetDir, "ZeroInstall.exe"),
                AppInfo.Current.Version,
                size,
                MachineWide);

            RegistryUtils.SetSoftwareString(@"Microsoft\PackageManagement", "ZeroInstall", Path.Combine(TargetDir, "ZeroInstall.OneGet.dll"), MachineWide);

            PathEnv.AddDir(TargetDir, MachineWide);

            Shortcut.Create(
                path: Shortcut.GetStartMenuPath("", "Zero Install", MachineWide),
                targetPath: Path.Combine(TargetDir, "ZeroInstall.exe"),
                appId: "ZeroInstall");
        }));
    }

    /// <summary>
    /// Removes Zero Install from the start menu and the PATH environment variable.
    /// </summary>
    private void DesktopIntegrationRemove()
    {
        if (WindowsUtils.IsWindows) DesktopIntegrationRemoveWindows();
    }

    [SupportedOSPlatform("windows")]
    private void DesktopIntegrationRemoveWindows()
    {
        Handler.RunTask(new SimpleTask(Resources.DesktopIntegrationRemove, () =>
        {
            string path = Shortcut.GetStartMenuPath("", "Zero Install", MachineWide);
            if (File.Exists(path)) File.Delete(path);

            PathEnv.RemoveDir(TargetDir, MachineWide);

            RegistryUtils.DeleteSoftwareValue(@"Microsoft\PackageManagement", "ZeroInstall", MachineWide);

            UninstallEntry.Unregister(UninstallID, MachineWide);
        }));
    }
}
