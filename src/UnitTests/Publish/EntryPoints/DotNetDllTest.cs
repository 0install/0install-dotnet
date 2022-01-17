// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// Contains test methods for <see cref="DotNetDll"/>.
/// </summary>
public class DotNetDllTest : CandidateTest
{
    public static readonly DotNetDll Reference = new()
    {
        RelativePath = "dotnet.dll",
        Name = "dotnet",
        Summary = "a Hello World application",
        Version = new("1.2.3.0"),
        RuntimeVersion = new("6.0.0"),
        NeedsTerminal = true
    };

    public static readonly DotNetDll ReferenceAspNetCore = new()
    {
        RelativePath = "dotnet-aspnetcore.dll",
        Name = "dotnet-aspnetcore",
        Summary = "a Hello World application",
        Version = new("1.2.3.0"),
        RuntimeVersion = new("6.0.0"),
        NeedsTerminal = true,
        NeedsAspNetCore = true
    };

    [Fact]
    public void CommandLine()
    {
        Deploy(Reference.RelativePath!.Replace(".dll", ".runtimeconfig.json"));
        TestAnalyze(Reference);
    }

    [Fact]
    public void AspNetCore()
    {
        Deploy(ReferenceAspNetCore.RelativePath!.Replace(".dll", ".runtimeconfig.json"));
        TestAnalyze(ReferenceAspNetCore);
    }
}
