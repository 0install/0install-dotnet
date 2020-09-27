// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using Microsoft.Win32;
using NanoByte.Common;
using ZeroInstall.Model;

namespace ZeroInstall.Commands.Desktop.SelfManagement
{
    partial class SelfManager
    {
        private const string UninstallRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        /// <summary>
        /// Adds uninstall information and OneGet registration to the registry.
        /// </summary>
        /// <param name="size">The size of the installed files in bytes.</param>
        /// <exception cref="UnauthorizedAccessException">Administrator rights are missing.</exception>
        private void RegistryApply(long size)
        {
            var hive = MachineWide ? Registry.LocalMachine : Registry.CurrentUser;
            using (var uninstallKey = hive.CreateSubKey(UninstallRegistryKey + @"\Zero Install_is1"))
            {
                if (uninstallKey == null) return;

                uninstallKey.SetValue("InstallLocation", TargetDir + @"\");
                uninstallKey.SetValue("Publisher", "0install.net");
                uninstallKey.SetValue("URLInfoAbout", "https://0install.net/");
                uninstallKey.SetValue("DisplayName", MachineWide ? "Zero Install" : "Zero Install (current user)");
                uninstallKey.SetValue("DisplayVersion", ImplementationVersion.ZeroInstall.ToString());
                uninstallKey.DeleteValue("MajorVersion", throwOnMissingValue: false);
                uninstallKey.DeleteValue("MinorVersion", throwOnMissingValue: false);
                uninstallKey.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
                uninstallKey.SetValue("EstimatedSize", size / 1024, RegistryValueKind.DWord);

                uninstallKey.SetValue("DisplayIcon", Path.Combine(TargetDir, "ZeroInstall.exe"));
                uninstallKey.SetValue("UninstallString", new[] {Path.Combine(TargetDir, "0install-win.exe"), Self.Name, Self.Remove.Name}.JoinEscapeArguments());
                uninstallKey.SetValue("QuietUninstallString", new[] {Path.Combine(TargetDir, "0install-win.exe"), Self.Name, Self.Remove.Name, "--batch", "--background"}.JoinEscapeArguments());
                uninstallKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
                uninstallKey.SetValue("NoRepiar", 1, RegistryValueKind.DWord);
            }

            RegistryUtils.SetSoftwareString("Zero Install", "InstallLocation", TargetDir, MachineWide);
            RegistryUtils.SetSoftwareString(@"Microsoft\PackageManagement", "ZeroInstall", Path.Combine(TargetDir, "ZeroInstall.OneGet.dll"), MachineWide);
        }

        /// <summary>
        /// Removes uninstall information and OneGet registration from the registry.
        /// </summary>
        private void RegistryRemove()
        {
            RegistryUtils.DeleteSoftwareValue("Zero Install", "InstallLocation", MachineWide);
            RegistryUtils.DeleteSoftwareValue(@"Microsoft\PackageManagement", "ZeroInstall", MachineWide);

            var hive = MachineWide ? Registry.LocalMachine : Registry.CurrentUser;
            using var uninstallKey = hive.CreateSubKey(UninstallRegistryKey);
            uninstallKey?.DeleteSubKey("Zero Install_is1", throwOnMissingSubKey: false);
        }
    }
}
