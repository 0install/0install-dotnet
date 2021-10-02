// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Info;
using NanoByte.Common.Native;
using ZeroInstall.DesktopIntegration.Windows;

namespace ZeroInstall.Commands.Desktop.SelfManagement
{
    partial class SelfManager
    {
        /// <summary>
        /// Adds uninstall information and OneGet registration to the registry.
        /// </summary>
        /// <param name="size">The size of the installed files in bytes.</param>
        /// <exception cref="UnauthorizedAccessException">Administrator rights are missing.</exception>
        private void RegistryApply(long size)
        {
            RegistryUtils.SetSoftwareString("Zero Install", "InstallLocation", TargetDir, MachineWide);
            RegistryUtils.SetSoftwareString(@"Microsoft\PackageManagement", "ZeroInstall", Path.Combine(TargetDir, "ZeroInstall.OneGet.dll"), MachineWide);
            UninstallEntry.Register(
                UninstallID,
                new[] { Path.Combine(TargetDir, "0install-win.exe"), Self.Name, Self.Remove.Name },
                "Zero Install",
                new("https://0install.net/"),
                iconPath: Path.Combine(TargetDir, "ZeroInstall.exe"),
                AppInfo.Current.Version,
                size,
                MachineWide);
        }

        /// <summary>
        /// Removes uninstall information and OneGet registration from the registry.
        /// </summary>
        private void RegistryRemove()
        {
            UninstallEntry.Unregister(UninstallID, MachineWide);
            RegistryUtils.DeleteSoftwareValue(@"Microsoft\PackageManagement", "ZeroInstall", MachineWide);
            RegistryUtils.DeleteSoftwareValue("Zero Install", "InstallLocation", MachineWide);
        }

        private const string UninstallID = "Zero Install_is1";
    }
}
