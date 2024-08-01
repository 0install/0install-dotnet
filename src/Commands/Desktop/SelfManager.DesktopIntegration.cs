// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using NanoByte.Common.Info;
using NanoByte.Common.Native;
using ZeroInstall.DesktopIntegration.Windows;

namespace ZeroInstall.Commands.Desktop;

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
        Handler.RunTask(new ActionTask(Resources.DesktopIntegrationApply, () =>
        {
            UninstallEntry.Register(
                UninstallID,
                [Path.Combine(TargetDir, "0install-win.exe"), Self.Name, Self.Remove.Name],
                "Zero Install",
                "0install.net",
                new("https://0install.net/"),
                iconPath: Path.Combine(TargetDir, "ZeroInstall.exe"),
                AppInfo.Current.Version,
                size,
                MachineWide);

            Shortcut.Create(
                path: Shortcut.GetStartMenuPath("", "Zero Install", MachineWide),
                targetPath: Path.Combine(TargetDir, "ZeroInstall.exe"),
                appId: "ZeroInstall");

            RegistryUtils.SetSoftwareString(@"Microsoft\PackageManagement", "ZeroInstall", Path.Combine(TargetDir, "ZeroInstall.OneGet.dll"), MachineWide);

            PathEnv.AddDir(TargetDir, MachineWide);
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
        Handler.RunTask(new ActionTask(Resources.DesktopIntegrationRemove, () =>
        {
            PathEnv.RemoveDir(TargetDir, MachineWide);

            RegistryUtils.DeleteSoftwareValue(@"Microsoft\PackageManagement", "ZeroInstall", MachineWide);

            string path = Shortcut.GetStartMenuPath("", "Zero Install", MachineWide);
            if (File.Exists(path)) File.Delete(path);

            UninstallEntry.Unregister(UninstallID, MachineWide);
        }));
    }
}
