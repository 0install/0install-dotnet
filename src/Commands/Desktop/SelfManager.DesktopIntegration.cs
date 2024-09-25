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
    /// <param name="libraryMode">Deploy Zero Install as a library for use by other applications. Do not create menu or uninstall entries.</param>
    private void DesktopIntegrationApply(long size, bool libraryMode)
    {
        if (WindowsUtils.IsWindows) DesktopIntegrationApplyWindows(size, libraryMode);
    }

    [SupportedOSPlatform("windows")]
    private void DesktopIntegrationApplyWindows(long size, bool libraryMode)
    {
        Handler.RunTask(new ActionTask(Resources.DesktopIntegrationApply, () =>
        {
            if (!libraryMode)
            {
                UninstallEntry.Register(
                    id: UninstallID,
                    name: "Zero Install",
                    uninstallCommand: [Path.Combine(TargetDir, "0install-win.exe"), Self.Name, Self.Remove.Name],
                    publisher: "0install.net",
                    homepage: new("https://0install.net/"),
                    iconPath: Path.Combine(TargetDir, "ZeroInstall.exe"),
                    version: AppInfo.Current.Version,
                    size: size,
                    machineWide: MachineWide);

                Shortcut.Create(
                    path: Shortcut.GetStartMenuPath("", "Zero Install", MachineWide),
                    targetPath: Path.Combine(TargetDir, "ZeroInstall.exe"),
                    appId: "ZeroInstall");

                RegistryUtils.SetSoftwareString(@"Microsoft\PackageManagement", "ZeroInstall", Path.Combine(TargetDir, "ZeroInstall.OneGet.dll"), MachineWide);
            }

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
