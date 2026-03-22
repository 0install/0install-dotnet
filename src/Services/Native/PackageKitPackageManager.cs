// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NET8_0_OR_GREATER

using System.Diagnostics;
using System.Runtime.Versioning;
using NanoByte.Common.Native;

namespace ZeroInstall.Services.Native;

/// <summary>
/// Detects native Linux packages (Debian and RPM) that are installed via PackageKit.
/// </summary>
/// <remarks>
/// This class queries installed packages using native package manager tools (dpkg-query for Debian, rpm for RPM).
/// Future enhancement: Use Tmds.DBus.Protocol to communicate with PackageKit via D-Bus for unified package management.
/// This class is immutable and thread-safe.
/// </remarks>
[SupportedOSPlatform("linux")]
public class PackageKitPackageManager : PackageManagerBase
{
    private readonly string _distributionName;

    public PackageKitPackageManager()
    {
        if (!UnixUtils.IsUnix) throw new PlatformNotSupportedException("PackageKit Package Manager can only be used on Linux platforms.");
        
        _distributionName = DetectDistribution();
    }

    /// <inheritdoc/>
    protected override string DistributionName => _distributionName;

    /// <inheritdoc/>
    protected override IEnumerable<ExternalImplementation> GetImplementations(string packageName)
    {
        if (string.IsNullOrEmpty(packageName)) throw new ArgumentException("Package name cannot be null or empty.", nameof(packageName));

        // TODO: Implement PackageKit D-Bus integration
        // This would require:
        // 1. Creating a transaction via org.freedesktop.PackageKit.CreateTransaction
        // 2. Calling Resolve on the transaction object with the package name
        // 3. Listening to Package signals to get package information
        // 4. Parsing package IDs and extracting version/architecture info

        // For now, use native package manager tools directly
        return _distributionName switch
        {
            KnownDistributions.Debian => QueryDebianPackage(packageName),
            KnownDistributions.Rpm => QueryRpmPackage(packageName),
            _ => []
        };
    }

    private string DetectDistribution()
    {
        // Detect the distribution type based on available package managers
        if (File.Exists("/usr/bin/dpkg") || File.Exists("/var/lib/dpkg/status"))
            return KnownDistributions.Debian;
        
        if (File.Exists("/usr/bin/rpm") || Directory.Exists("/var/lib/rpm"))
            return KnownDistributions.Rpm;
        
        // Default to Debian as a fallback
        return KnownDistributions.Debian;
    }

    private IEnumerable<ExternalImplementation> QueryDebianPackage(string packageName)
    {
        Process? process = null;
        try
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/dpkg-query",
                    Arguments = $"-W -f='${{Status}}|${{Version}}|${{Architecture}}' {packageName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (!File.Exists(process.StartInfo.FileName)) yield break;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0 && output.Contains("install ok installed"))
            {
                var parts = output.Trim('\'').Split('|');
                if (parts.Length >= 3)
                {
                    string versionString = parts[1].Trim();
                    // Remove epoch from version if present (e.g., "1:2.0" -> "2.0")
                    if (versionString.Contains(':'))
                        versionString = versionString.Substring(versionString.IndexOf(':') + 1);
                    
                    // Remove Debian revision if present (e.g., "2.0-1" -> "2.0")
                    if (versionString.Contains('-'))
                        versionString = versionString.Substring(0, versionString.IndexOf('-'));

                    if (ImplementationVersion.TryCreate(versionString, out var version))
                    {
                        var arch = parts[2].Trim();
                        var cpu = MapDebianArchToCpu(arch);

                        yield return new ExternalImplementation(DistributionName, packageName, version, cpu)
                        {
                            IsInstalled = true,
                            QuickTestFile = $"/var/lib/dpkg/info/{packageName}.list"
                        };
                    }
                }
            }
        }
        finally
        {
            process?.Dispose();
        }
    }

    private IEnumerable<ExternalImplementation> QueryRpmPackage(string packageName)
    {
        Process? process = null;
        try
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/rpm",
                    Arguments = $"-q --queryformat '%{{VERSION}}|%{{ARCH}}' {packageName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (!File.Exists(process.StartInfo.FileName)) yield break;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0 && !output.Contains("is not installed"))
            {
                var parts = output.Trim('\'').Split('|');
                if (parts.Length >= 2 && ImplementationVersion.TryCreate(parts[0].Trim(), out var version))
                {
                    var arch = parts[1].Trim();
                    var cpu = MapRpmArchToCpu(arch);

                    yield return new ExternalImplementation(DistributionName, packageName, version, cpu)
                    {
                        IsInstalled = true
                    };
                }
            }
        }
        finally
        {
            process?.Dispose();
        }
    }

    private static Cpu MapDebianArchToCpu(string arch) => arch switch
    {
        "amd64" or "x86_64" => Cpu.X64,
        "i386" or "i686" => Cpu.I486,
        "arm64" or "aarch64" => Cpu.AArch64,
        "armhf" or "armv7l" => Cpu.ArmV7L,
        _ => Cpu.All
    };

    private static Cpu MapRpmArchToCpu(string arch) => arch switch
    {
        "x86_64" => Cpu.X64,
        "i386" or "i686" => Cpu.I486,
        "aarch64" => Cpu.AArch64,
        "armv7hl" or "armv7l" => Cpu.ArmV7L,
        _ => Cpu.All
    };
}

#endif
