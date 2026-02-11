// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using NanoByte.Common.Native;
using NanoByte.Common.Threading;

namespace ZeroInstall.Services.Native;

/// <summary>
/// Detects and installs packages via Windows Package Manager (WinGet).
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
[SupportedOSPlatform("windows")]
public class WinGetPackageManager : PackageManagerBase
{
    private static readonly Regex _versionRegex = new(@"\d+(?:\.\d+)+(?:-[a-zA-Z0-9.]+)?", RegexOptions.Compiled);
    private readonly ITaskHandler _handler;

    public WinGetPackageManager(ITaskHandler handler)
    {
        if (!WindowsUtils.IsWindows) throw new PlatformNotSupportedException("WinGet Package Manager can only be used on the Windows platform.");
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <inheritdoc/>
    protected override string DistributionName => KnownDistributions.WinGet;

    /// <summary>
    /// Escapes an argument for safe use in command line.
    /// </summary>
    private static string EscapeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg)) return "\"\"";
        
        // If argument contains space, tab, or double quote, it needs to be quoted
        if (arg.IndexOfAny(new[] { ' ', '\t', '"' }) == -1)
            return arg;

        // Escape internal quotes and wrap in quotes
        return "\"" + arg.Replace("\"", "\\\"") + "\"";
    }

    /// <summary>
    /// Builds a safe argument string from multiple arguments.
    /// </summary>
    private static string BuildArguments(params string[] args)
    {
        return string.Join(" ", args.Select(EscapeArgument));
    }

    /// <inheritdoc/>
    protected override IEnumerable<ExternalImplementation> GetImplementations(string packageName)
    {
        if (string.IsNullOrEmpty(packageName)) throw new ArgumentNullException(nameof(packageName));

        // Check if winget is available
        if (!IsWinGetAvailable()) return [];

        // Query winget for installed packages
        var installedPackages = QueryInstalledPackages(packageName).ToList();

        // Query winget for available (not installed) packages
        var availablePackages = QueryAvailablePackages(packageName).ToList();

        return installedPackages.Concat(availablePackages);
    }

    private bool IsWinGetAvailable()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;
            
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private IEnumerable<ExternalImplementation> QueryInstalledPackages(string packageName)
    {
        var results = new List<ExternalImplementation>();
        
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = BuildArguments("list", "--disable-interactivity", "--accept-source-agreements", 
                    "--source", "winget", "--exact", "--id", packageName),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process == null) return results;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0) return results;

            // Parse the output to extract version information
            var versions = ParseWinGetListOutput(output, packageName);
            foreach (var version in versions)
            {
                results.Add(new ExternalImplementation(DistributionName, packageName, version, Architecture.CurrentSystem.Cpu)
                {
                    IsInstalled = true
                });
            }
        }
        catch (Exception ex)
        {
            Log.Debug($"Failed to query installed WinGet packages for {packageName}: {ex.Message}");
        }
        
        return results;
    }

    private IEnumerable<ExternalImplementation> QueryAvailablePackages(string packageName)
    {
        var results = new List<ExternalImplementation>();
        
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = BuildArguments("show", "--disable-interactivity", "--accept-source-agreements",
                    "--source", "winget", "--exact", "--id", packageName, "--versions"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process == null) return results;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0) return results;

            // Parse the output to extract available versions
            var versions = ParseWinGetShowVersions(output);
            foreach (var version in versions)
            {
                results.Add(new ExternalImplementation(DistributionName, packageName, version, Architecture.CurrentSystem.Cpu)
                {
                    IsInstalled = false,
                    RetrievalMethods =
                    {
                        new ExternalRetrievalMethod
                        {
                            Distro = DistributionName,
                            PackageID = packageName,
                            ConfirmationQuestion = $"Install {packageName} version {version} via WinGet?",
                            Install = () => InstallPackage(packageName, version)
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Log.Debug($"Failed to query available WinGet packages for {packageName}: {ex.Message}");
        }
        
        return results;
    }

    private void InstallPackage(string packageName, ImplementationVersion version)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = BuildArguments("install", "--disable-interactivity", "--accept-source-agreements",
                    "--accept-package-agreements", "--source", "winget", "--exact", "--id", packageName,
                    "--version", version.ToString()),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false // Show window so user can see progress
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                throw new IOException($"Failed to start winget to install {packageName}");

            // Read both streams asynchronously to avoid deadlock
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            process.WaitForExit();
            
            if (process.ExitCode != 0)
            {
                string error = errorTask.Result;
                throw new IOException($"WinGet installation failed with exit code {process.ExitCode}: {error}");
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to install {packageName} via WinGet: {ex.Message}", ex);
        }
    }

    private static IEnumerable<ImplementationVersion> ParseWinGetListOutput(string output, string packageId)
    {
        // WinGet list output format (example):
        // Name                                     Id                     Version      Available    Source
        // ------------------------------------------------------------------------------------------------------------------------
        // Python 3.12                              Python.Python.3.12     3.12.1       3.12.2       winget
        //
        // Note: This is a simple text-based parser. For more robust parsing, consider using
        // WinGet's JSON output format (--output json) when it becomes more stable.
        
        var lines = output.Split('\n');
        bool foundHeader = false;
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            // Skip until we find the separator line
            if (line.Contains("----"))
            {
                foundHeader = true;
                continue;
            }
            
            if (!foundHeader) continue;
            
            // Split line into fields by whitespace (simple approach)
            var fields = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Look for the package ID in the fields
            bool foundPackageId = false;
            int idFieldIndex = -1;
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], packageId, StringComparison.OrdinalIgnoreCase))
                {
                    foundPackageId = true;
                    idFieldIndex = i;
                    break;
                }
            }
            
            if (!foundPackageId) continue;
            
            // The version field typically follows the ID field
            // Try to parse the next field(s) as version
            for (int i = idFieldIndex + 1; i < fields.Length; i++)
            {
                if (ImplementationVersion.TryCreate(fields[i], out var version))
                {
                    yield return version;
                    break; // Only return the first valid version per line
                }
            }
        }
    }

    private static IEnumerable<ImplementationVersion> ParseWinGetShowVersions(string output)
    {
        // WinGet show --versions output format (example):
        // Available versions:
        // 3.12.2
        // 3.12.1
        // 3.12.0
        
        var lines = output.Split('\n');
        bool inVersionSection = false;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            
            // Look for "Available versions:" or similar header
            if (trimmed.IndexOf("version", StringComparison.OrdinalIgnoreCase) >= 0 && trimmed.EndsWith(":"))
            {
                inVersionSection = true;
                continue;
            }
            
            if (!inVersionSection) continue;
            
            // Try to parse as version
            if (ImplementationVersion.TryCreate(trimmed, out var version))
            {
                yield return version;
            }
        }
    }
}
