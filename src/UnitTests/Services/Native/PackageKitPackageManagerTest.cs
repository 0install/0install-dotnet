// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NET8_0_OR_GREATER

using System.Diagnostics;
using System.Runtime.Versioning;
using NanoByte.Common.Native;

namespace ZeroInstall.Services.Native;

[SupportedOSPlatform("linux")]
public class PackageKitPackageManagerTest
{
    private readonly PackageKitPackageManager? _packageManager;

    public PackageKitPackageManagerTest()
    {
        Skip.IfNot(UnixUtils.IsUnix);
        Skip.IfNot(OperatingSystem.IsLinux());
        _packageManager = new();
    }

    [SkippableFact]
    public void DetectsDistribution()
    {
        _packageManager.Should().NotBeNull();
    }

    [SkippableFact]
    public void QueriesInstalledDebianPackage()
    {
        Skip.IfNot(File.Exists("/usr/bin/dpkg"));

        // Test with a commonly installed package on Debian-based systems
        var implementations = _packageManager!.Query(
            new() { Package = "bash", Distributions = { KnownDistributions.Debian } },
            KnownDistributions.Debian);

        // If bash is installed (which it should be on most systems), we should get at least one implementation
        implementations.Should().NotBeEmpty();
        implementations.Should().OnlyContain(impl =>
            impl.Package == "bash"
         && impl.IsInstalled
         && impl.Version != null);
    }

    [SkippableFact]
    public void QueriesInstalledRpmPackage()
    {
        Skip.IfNot(File.Exists("/usr/bin/rpm"));

        // Check if there are any RPM packages installed
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/rpm",
                Arguments = "-qa",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        Skip.If(string.IsNullOrWhiteSpace(output), "No RPM packages installed on this system");

        // Test with a commonly installed package on RPM-based systems
        var implementations = _packageManager!.Query(
            new() { Package = "bash", Distributions = { KnownDistributions.Rpm } },
            KnownDistributions.Rpm);

        // If bash is installed (which it should be on most systems), we should get at least one implementation
        implementations.Should().NotBeEmpty();
        implementations.Should().OnlyContain(impl =>
            impl.Package == "bash"
         && impl.IsInstalled
         && impl.Version != null);
    }

    [SkippableFact]
    public void ReturnsEmptyForNonExistentPackage()
    {
        Skip.IfNot(File.Exists("/usr/bin/dpkg") || File.Exists("/usr/bin/rpm"));

        string distribution = File.Exists("/usr/bin/dpkg") ? KnownDistributions.Debian : KnownDistributions.Rpm;

        var implementations = _packageManager!.Query(
            new() { Package = "nonexistentpackagename12345", Distributions = { distribution } },
            distribution);

        implementations.Should().BeEmpty();
    }

    [SkippableFact]
    public void HandlesVersionFiltering()
    {
        Skip.IfNot(File.Exists("/usr/bin/dpkg") || File.Exists("/usr/bin/rpm"));

        string distribution = File.Exists("/usr/bin/dpkg") ? KnownDistributions.Debian : KnownDistributions.Rpm;

        // Query for bash with a very high version requirement that likely won't match
        var implementations = _packageManager!.Query(
            new() 
            { 
                Package = "bash", 
                Distributions = { distribution },
                Version = new VersionRange("999.0..") // Requires version 999.0 or higher
            },
            distribution);

        // Should be empty since no bash version 999.0 exists
        implementations.Should().BeEmpty();
    }
}

#endif
