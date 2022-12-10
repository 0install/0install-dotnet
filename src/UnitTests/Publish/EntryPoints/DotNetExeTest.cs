// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// Contains test methods for <see cref="DotNetExe"/>.
/// </summary>
public class DotNetExeTest : CandidateTest
{
    public static readonly DotNetExe Reference = new()
    {
        RelativePath = "dotnet.exe",
        Name = "dotnet",
        Summary = "a Hello World application",
        Version = new("1.2.3.0"),
        RuntimeVersion = new("6.0.0"),
        NeedsTerminal = true
    };

    public static readonly DotNetExe ReferenceAspNetCore = new()
    {
        RelativePath = "dotnet-aspnetcore.exe",
        Name = "dotnet-aspnetcore",
        Summary = "a Hello World application",
        Version = new("1.2.3.0"),
        RuntimeVersion = new("6.0.0"),
        NeedsTerminal = true,
        NeedsAspNetCore = true
    };

    public static readonly DotNetExe ReferenceWindowsDesktop = new()
    {
        RelativePath = "dotnet-windowsdesktop.exe",
        Name = "dotnet-windowsdesktop",
        Summary = "a Hello World application",
        Version = new("1.2.3.0"),
        RuntimeVersion = new("6.0.0")
    };

    public DotNetExeTest()
    {
        Skip.IfNot(WindowsUtils.IsWindows, reason: "Non-Windows systems cannot parse PE headers.");
    }

    [SkippableFact]
    public void CommandLine()
    {
        Deploy($"{Reference.RelativePath![..^4]}.runtimeconfig.json");
        Deploy(DotNetDllTest.Reference);
        TestAnalyze(Reference);
    }

    [SkippableFact]
    public void AspNetCore()
    {
        Deploy($"{ReferenceAspNetCore.RelativePath![..^4]}.runtimeconfig.json");
        Deploy(DotNetDllTest.ReferenceAspNetCore);
        TestAnalyze(ReferenceAspNetCore);
    }

    [SkippableFact]
    public void WindowsDesktop()
    {
        Deploy($"{ReferenceWindowsDesktop.RelativePath![..^4]}.runtimeconfig.json");
        Deploy(ReferenceWindowsDesktop.RelativePath.Replace(".exe", ".dll"));
        Deploy(ReferenceWindowsDesktop);
        TestAnalyze(ReferenceWindowsDesktop);
    }
}
