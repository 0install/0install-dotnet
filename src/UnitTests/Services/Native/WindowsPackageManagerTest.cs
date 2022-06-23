// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using NanoByte.Common.Native;
using static System.Runtime.InteropServices.Architecture;
using static System.Runtime.InteropServices.RuntimeInformation;

namespace ZeroInstall.Services.Native;

[SupportedOSPlatform("windows")]
public class WindowsPackageManagerTest
{
    private readonly WindowsPackageManager _packageManager;

    public WindowsPackageManagerTest()
    {
        Skip.IfNot(WindowsUtils.IsWindows);
        Skip.If(ProcessArchitecture == X86);
        _packageManager = new();
    }

    [SkippableFact]
    public void DotNetFramework()
        => ExpectImplementation("netfx", commandPath: "", quickTest: @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll");

    [SkippableFact]
    public void DotNetFrameworkX86()
        => ExpectImplementation("netfx", commandPath: "", quickTest: @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\mscorlib.dll");

    [SkippableFact]
    public void DotNetFrameworkClientProfile()
        => ExpectImplementation("netfx-client", commandPath: "", quickTest: @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll");

    [SkippableFact]
    public void PowerShell()
        => ExpectImplementation("powershell", commandPath: @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", quickTest: @"System32\WindowsPowerShell\v1.0\powershell.exe");

    [SkippableFact]
    public void PowerShellX86()
        => ExpectImplementation("powershell", commandPath: @"C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe", quickTest: @"SysWOW64\WindowsPowerShell\v1.0\powershell.exe");

    [SkippableFact]
    public void DotNetRuntime()
        => ExpectImplementation("dotnet-runtime", commandPath: @"C:\Program Files\dotnet\dotnet.exe", quickTest: @"dotnet\shared\Microsoft.NETCore.App");

    [SkippableFact]
    public void DotNetAspNetCoreRuntime()
        => ExpectImplementation("dotnet-aspnetcore-runtime", commandPath: @"C:\Program Files\dotnet\dotnet.exe", quickTest: @"dotnet\shared\Microsoft.AspNetCore.App");

    [SkippableFact]
    public void DotNetWindowsDesktopRuntime()
        => ExpectImplementation("dotnet-windowsdesktop-runtime", commandPath: @"C:\Program Files\dotnet\dotnet.exe", quickTest: @"dotnet\shared\Microsoft.WindowsDesktop.App");

    [SkippableFact]
    public void DotNetSdk() => ExpectImplementation("dotnet-sdk", commandPath: @"C:\Program Files\dotnet\dotnet.exe", quickTest: @"dotnet\sdk");

    private void ExpectImplementation(string packageName, string commandPath, string quickTest)
    {
        var implementations = _packageManager.Query(new() {Package = packageName, Distributions = {"Windows"}}, "Windows");
        implementations.Should().Contain(impl =>
            impl.Package == packageName
         && impl.IsInstalled
         && impl.Commands.Any(x => x.Name == Command.NameRun && x.Path != null && StringUtils.EqualsIgnoreCase(x.Path, commandPath))
         && impl.QuickTestFile != null
         && impl.QuickTestFile.ContainsIgnoreCase(quickTest));
    }
}
