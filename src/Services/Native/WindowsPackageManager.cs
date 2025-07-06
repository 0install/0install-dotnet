// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using System.Runtime.Versioning;
using NanoByte.Common.Native;
using static System.Environment;
using static System.Runtime.InteropServices.Architecture;
using static System.Runtime.InteropServices.RuntimeInformation;

namespace ZeroInstall.Services.Native;

/// <summary>
/// Detects common Windows software packages (such as Java and .NET) that are installed natively.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
[SupportedOSPlatform("windows")]
public class WindowsPackageManager : PackageManagerBase
{
    public WindowsPackageManager()
    {
        if (!WindowsUtils.IsWindows) throw new PlatformNotSupportedException("Windows Package Manager can only be used on the Windows platform.");
    }

    /// <inheritdoc/>
    protected override string DistributionName => KnownDistributions.Windows;

    /// <inheritdoc/>
    protected override IEnumerable<ExternalImplementation> GetImplementations(string packageName)
        => (packageName ?? throw new ArgumentNullException(nameof(packageName))) switch
        {
            _ when packageName.StartsWith("openjdk-", out string? rest)
                && rest.EndsWith("-jre", out rest)
                && int.TryParse(rest, out int version) => FindJre(version),
            _ when packageName.StartsWith("openjdk-", out string? rest)
                && rest.EndsWith("-jdk", out rest)
                && int.TryParse(rest, out int version) => FindJdk(version),
            "netfx" =>
            [
                // See: https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
                ..FindNetFx("2.0", WindowsUtils.NetFx20, WindowsUtils.NetFx20),
                ..FindNetFx("3.0", WindowsUtils.NetFx20, WindowsUtils.NetFx30),
                ..FindNetFx("3.5", WindowsUtils.NetFx20, WindowsUtils.NetFx35),
                ..FindNetFx("4.0", WindowsUtils.NetFx40, @"v4\Full"),
                ..FindNetFx("4.5", WindowsUtils.NetFx40, @"v4\Full", 378389),
                ..FindNetFx("4.5.1", WindowsUtils.NetFx40, @"v4\Full", 378675), // also covers 378758
                ..FindNetFx("4.5.2", WindowsUtils.NetFx40, @"v4\Full", 379893),
                ..FindNetFx("4.6", WindowsUtils.NetFx40, @"v4\Full", 393295), // also covers 393297
                ..FindNetFx("4.6.1", WindowsUtils.NetFx40, @"v4\Full", 394254),
                ..FindNetFx("4.6.2", WindowsUtils.NetFx40, @"v4\Full", 394802), // also covers 394806
                ..FindNetFx("4.7", WindowsUtils.NetFx40, @"v4\Full", 460798), // also covers 460805
                ..FindNetFx("4.7.1", WindowsUtils.NetFx40, @"v4\Full", 461308), // also covers 461310
                ..FindNetFx("4.7.2", WindowsUtils.NetFx40, @"v4\Full", 461808), // also covers 461814
                ..FindNetFx("4.8", WindowsUtils.NetFx40, @"v4\Full", 528040), // also covers 528049, 528372 and 528449
                ..FindNetFx("4.8.1", WindowsUtils.NetFx40, @"v4\Full", 533320) // also covers 533325
            ],
            "netfx-client" => FindNetFx("4.0", WindowsUtils.NetFx40, @"v4\Client"),
            "powershell" => FindPowerShell(),
            _ when packageName.StartsWith("dotnet-") =>
            [
                ..FindDotNet(packageName, SpecialFolder.ProgramFiles, (ProcessArchitecture == X86) ? Cpu.I486 : Architecture.CurrentSystem.Cpu),
                ..FindDotNet(packageName, SpecialFolder.ProgramFilesX86, Cpu.I486),
            ],
            "git" => FindGitForWindows(),
            _ => []
        };

    private IEnumerable<ExternalImplementation> FindJre(int version) => FindJava(version,
        typeShort: "jre",
        typeLong: "Java Runtime Environment",
        mainExe: "java",
        secondaryCommand: Command.NameRunGui,
        secondaryExe: "javaw");

    private IEnumerable<ExternalImplementation> FindJdk(int version) => FindJava(version,
        typeShort: "jdk",
        typeLong: "Java Development Kit",
        mainExe: "javac",
        secondaryCommand: "java",
        secondaryExe: "java");

    private IEnumerable<ExternalImplementation> FindJava(int version, string typeShort, string typeLong, string mainExe, string secondaryCommand, string secondaryExe)
        => from javaHome in GetRegisteredPaths($@"JavaSoft\{typeLong}\1.{version}", "JavaHome")
           let mainPath = Path.Combine(javaHome.path, $@"bin\{mainExe}.exe")
           let secondaryPath = Path.Combine(javaHome.path, $@"bin\{secondaryExe}.exe")
           where File.Exists(mainPath) && File.Exists(secondaryPath)
           select new ExternalImplementation(DistributionName, $"openjdk-{version}-{typeShort}",
               new ImplementationVersion(FileVersionInfo.GetVersionInfo(mainPath).ProductVersion!.GetLeftPartAtLastOccurrence(".")), // Trim patch level
               javaHome.cpu)
           {
               Commands =
               {
                   new() {Name = Command.NameRun, Path = mainPath},
                   new() {Name = secondaryCommand, Path = secondaryPath}
               },
               IsInstalled = true,
               QuickTestFile = mainPath
           };

    private static IEnumerable<(Cpu cpu, string path)> GetRegisteredPaths(string registrySuffix, string valueName)
    {
        // Check for system native architecture (may be 32-bit or 64-bit)
        string? path = RegistryUtils.GetString($@"HKEY_LOCAL_MACHINE\SOFTWARE\{registrySuffix}", valueName);
        if (!string.IsNullOrEmpty(path))
            yield return (Architecture.CurrentSystem.Cpu, path);

        // Check for 32-bit on a 64-bit system
        if (Is64BitProcess)
        {
            path = RegistryUtils.GetString($@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\{registrySuffix}", valueName);
            if (!string.IsNullOrEmpty(path))
                yield return (Cpu.I486, path);
        }
    }

    // Uses detection logic described here: http://msdn.microsoft.com/library/hh925568
    private IEnumerable<ExternalImplementation> FindNetFx(string version, string clrVersion, string registryVersion, int releaseNumber = 0)
    {
        ExternalImplementation Impl(Cpu cpu) => new(DistributionName, registryVersion.EndsWith("Client") ? "netfx-client" : "netfx", new(version), cpu)
        {
            // .NET executables do not need a runner on Windows
            Commands = {new() {Name = Command.NameRun, Path = ""}},
            IsInstalled = true,
            QuickTestFile = Path.Combine(WindowsUtils.GetNetFxDirectory(clrVersion).Replace("Framework64", cpu == Cpu.I486 ? "Framework" : "Framework64"), "mscorlib.dll")
        };

        // Check for system native architecture (may be 32-bit or 64-bit)
        int install = RegistryUtils.GetDword($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\{registryVersion}", "Install");
        int release = RegistryUtils.GetDword($@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\{registryVersion}", "Release");
        if (install == 1 && release >= releaseNumber)
            yield return Impl(Architecture.CurrentSystem.Cpu);

        // Check for 32-bit on a 64-bit system
        if (Is64BitProcess)
        {
            install = RegistryUtils.GetDword($@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\NET Framework Setup\NDP\{registryVersion}", "Install");
            release = RegistryUtils.GetDword($@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\NET Framework Setup\NDP\{registryVersion}", "Release");
            if (install == 1 && release >= releaseNumber)
                yield return Impl(Cpu.I486);
        }
    }

    private IEnumerable<ExternalImplementation> FindDotNet(string packageName, SpecialFolder folder, Cpu cpu)
    {
        string rootPath = Path.Combine(WindowsUtils.GetFolderPath(folder), "dotnet");

        string? packageDir = packageName switch
        {
            "dotnet-runtime" => Path.Combine("shared", "Microsoft.NETCore.App"),
            "dotnet-aspnetcore-runtime" => Path.Combine("shared", "Microsoft.AspNetCore.App"),
            "dotnet-windowsdesktop-runtime" => Path.Combine("shared", "Microsoft.WindowsDesktop.App"),
            "dotnet-sdk" => "sdk",
            _ => null
        };
        if (packageDir == null) yield break;

        string componentPath = Path.Combine(rootPath, packageDir);
        if (!Directory.Exists(componentPath)) yield break;
        foreach (string path in Directory.GetDirectories(componentPath))
        {
            if (ImplementationVersion.TryCreate(Path.GetFileName(path), out var version))
            {
                yield return new(DistributionName, packageName, version, cpu)
                {
                    Commands = {new() {Name = Command.NameRun, Path = Path.Combine(rootPath, "dotnet.exe")}},
                    IsInstalled = true,
                    QuickTestFile = Path.Combine(path, ".version")
                };
            }
        }
    }

    private IEnumerable<ExternalImplementation> FindPowerShell()
    {
        ExternalImplementation? Impl(string baseVersion, bool wow6432)
        {
            string regPrefix = $@"HKEY_LOCAL_MACHINE\SOFTWARE\{(wow6432 ? @"Wow6432Node\" : "")}Microsoft\PowerShell\{baseVersion}";
            if (RegistryUtils.GetDword(regPrefix, "Install") != 1) return null;

            string? version = RegistryUtils.GetString($@"{regPrefix}\PowerShellEngine", "PowerShellVersion");
            string? path = RegistryUtils.GetString($@"{regPrefix}\PowerShellEngine", "ApplicationBase");
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(path)) return null;

            return new(DistributionName, "powershell",
                new ImplementationVersion(version),
                wow6432 ? Cpu.I486 : Architecture.CurrentSystem.Cpu)
            {
                Commands = {new() {Name = Command.NameRun, Path = Path.Combine(path, "powershell.exe")}},
                QuickTestFile = Path.Combine(path, "powershell.exe"),
                IsInstalled = true
            };
        }

        if ((Impl(baseVersion: "3", wow6432: false) ?? Impl(baseVersion: "1", wow6432: false)) is {} impl)
            yield return impl;

        if (Is64BitProcess && (Impl(baseVersion: "3", wow6432: true) ?? Impl(baseVersion: "1", wow6432: true)) is {} impl64)
            yield return impl64;
    }

    private IEnumerable<ExternalImplementation> FindGitForWindows()
    {
        ExternalImplementation? Impl(bool wow6432)
        {
            string regKey = $@"HKEY_LOCAL_MACHINE\SOFTWARE\{(wow6432 ? @"Wow6432Node\" : "")}GitForWindows";
            string? version = RegistryUtils.GetString(regKey, "CurrentVersion");
            string? path = RegistryUtils.GetString(regKey, "InstallPath");
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(path)) return null;

            return new(DistributionName, "git",
                new ImplementationVersion(version),
                wow6432 ? Cpu.I486 : Architecture.CurrentSystem.Cpu)
            {
                Commands =
                {
                    new() {Name = Command.NameRun, Path = Path.Combine(path, @"cmd\git.exe")},
                    new() {Name = Command.NameRunGui, Path = Path.Combine(path, @"cmd\git-gui.exe")},
                    new() {Name = "gitk", Path = Path.Combine(path, @"cmd\gitk.exe")},
                    new() {Name = "start-ssh-agent", Path = Path.Combine(path, @"cmd\start-ssh-agent.exe")},
                    new() {Name = "git-bash", Path = Path.Combine(path, @"git-bash.exe")},
                    new() {Name = "git-cmd", Path = Path.Combine(path, @"git-cmd.exe")},
                    new() {Name = "bash", Path = Path.Combine(path, @"usr\bin\bash.exe")},
                    new() {Name = "sh", Path = Path.Combine(path, @"usr\bin\sh.exe")},
                    new() {Name = "ssh", Path = Path.Combine(path, @"usr\bin\ssh.exe")},
                    new() {Name = "scp", Path = Path.Combine(path, @"usr\bin\scp.exe")},
                    new() {Name = "gpg", Path = Path.Combine(path, @"usr\bin\gpg.exe")},
                    new() {Name = "gpgv", Path = Path.Combine(path, @"usr\bin\gpgv.exe")},
                    new() {Name = "gpgsplit", Path = Path.Combine(path, @"usr\bin\gpgsplit.exe")}
                },
                IsInstalled = true
            };
        }

        var impl = Impl(wow6432: false);
        if (impl != null) yield return impl;

        if (Is64BitProcess)
        {
            impl = Impl(wow6432: true);
            if (impl != null) yield return impl;
        }
    }
}
