// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.Versioning;
using NanoByte.Common.Native;
using NanoByte.Common.Tasks;
using ZeroInstall.Model.Selection;

namespace ZeroInstall.Services.Native;

[SupportedOSPlatform("windows")]
public class WinGetPackageManagerTest
{
    private readonly WinGetPackageManager? _packageManager;

    public WinGetPackageManagerTest()
    {
        Skip.IfNot(WindowsUtils.IsWindows, "WinGet is only available on Windows");
        
        // Check if winget is available
        if (!IsWinGetAvailable())
        {
            Skip.If(true, "WinGet is not installed on this system");
        }
        else
        {
            _packageManager = new WinGetPackageManager(new SilentTaskHandler());
        }
    }

    private static bool IsWinGetAvailable()
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "winget",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null) return false;
            
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    [SkippableFact]
    public void QueryReturnsEmptyForUnknownPackage()
    {
        Skip.If(_packageManager == null);
        
        var implementations = _packageManager!.Query(
            new() { Package = "NonExistentPackage12345XYZ", Distributions = { KnownDistributions.WinGet } },
            KnownDistributions.WinGet);
        
        implementations.Should().BeEmpty();
    }

    [SkippableFact]
    public void QueryReturnsImplementationsForKnownPackage()
    {
        Skip.If(_packageManager == null);
        
        // Use a well-known package that's likely to be available in WinGet
        // Git.Git is a common package in WinGet
        var implementations = _packageManager!.Query(
            new() { Package = "Git.Git", Distributions = { KnownDistributions.WinGet } },
            KnownDistributions.WinGet);
        
        // Should return at least some implementations (installed or available)
        implementations.Should().NotBeEmpty();
        
        // All implementations should have the correct distribution and package name
        implementations.Should().AllSatisfy(impl =>
        {
            impl.Distribution.Should().Be(KnownDistributions.WinGet);
            impl.Package.Should().Be("Git.Git");
            impl.Version.Should().NotBeNull();
        });
    }

    [SkippableFact]
    public void QueryFiltersOutNonWinGetDistribution()
    {
        Skip.If(_packageManager == null);
        
        // Query with wrong distribution
        var implementations = _packageManager!.Query(
            new() { Package = "Git.Git", Distributions = { KnownDistributions.Windows } },
            KnownDistributions.Windows);
        
        implementations.Should().BeEmpty();
    }

    [SkippableFact]
    public void InstalledPackagesHaveIsInstalledTrue()
    {
        Skip.If(_packageManager == null);
        
        // Query for all implementations
        var implementations = _packageManager!.Query(
            new() { Package = "Git.Git", Distributions = { KnownDistributions.WinGet } },
            KnownDistributions.WinGet).ToList();
        
        // If there are installed packages, they should have IsInstalled = true
        var installedPackages = implementations.Where(impl => impl.IsInstalled).ToList();
        if (installedPackages.Any())
        {
            installedPackages.Should().AllSatisfy(impl =>
            {
                impl.IsInstalled.Should().BeTrue();
                impl.RetrievalMethods.Should().BeEmpty(); // Installed packages shouldn't have retrieval methods
            });
        }
    }

    [SkippableFact]
    public void AvailablePackagesHaveRetrievalMethod()
    {
        Skip.If(_packageManager == null);
        
        // Query for all implementations
        var implementations = _packageManager!.Query(
            new() { Package = "Git.Git", Distributions = { KnownDistributions.WinGet } },
            KnownDistributions.WinGet).ToList();
        
        // Available (not installed) packages should have retrieval methods
        var availablePackages = implementations.Where(impl => !impl.IsInstalled).ToList();
        if (availablePackages.Any())
        {
            availablePackages.Should().AllSatisfy(impl =>
            {
                impl.IsInstalled.Should().BeFalse();
                impl.RetrievalMethods.Should().ContainSingle();
                
                var retrievalMethod = impl.RetrievalMethods.First() as ExternalRetrievalMethod;
                retrievalMethod.Should().NotBeNull();
                retrievalMethod!.Distro.Should().Be(KnownDistributions.WinGet);
                retrievalMethod.PackageID.Should().Be("Git.Git");
                retrievalMethod.ConfirmationQuestion.Should().NotBeNullOrEmpty();
                retrievalMethod.Install.Should().NotBeNull();
            });
        }
    }

    [SkippableFact]
    public void LookupReturnsNullForNonWinGetPackage()
    {
        Skip.If(_packageManager == null);
        
        var selection = new ImplementationSelection 
        { 
            ID = "package:windows:netfx:4.8",
            InterfaceUri = new("https://example.com/test"),
            Version = new("4.8")
        };
        var result = _packageManager!.Lookup(selection);
        
        result.Should().BeNull();
    }

    [SkippableFact]
    public void LookupReturnsImplementationForWinGetPackage()
    {
        Skip.If(_packageManager == null);
        
        // First query to get an actual implementation
        var implementations = _packageManager!.Query(
            new() { Package = "Git.Git", Distributions = { KnownDistributions.WinGet } },
            KnownDistributions.WinGet).ToList();
        
        Skip.If(!implementations.Any(), "No Git.Git implementations found");
        
        var firstImpl = implementations.First();
        var selection = new ImplementationSelection 
        { 
            ID = firstImpl.ID,
            InterfaceUri = new("https://example.com/test"),
            Version = firstImpl.Version
        };
        
        var result = _packageManager.Lookup(selection);
        
        result.Should().NotBeNull();
        result!.Distribution.Should().Be(KnownDistributions.WinGet);
        result.Package.Should().Be("Git.Git");
    }
}
