// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Executes Linux binaries on Windows using Windows Subsystem for Linux (WSL).
/// </summary>
public class WSLStrategy : IExecutionStrategy
{
    private readonly string _distribution;

    /// <summary>
    /// Creates a new WSL execution strategy.
    /// </summary>
    /// <param name="distribution">The WSL distribution to use (e.g., "Ubuntu"). If null, uses the default distribution.</param>
    public WSLStrategy(string? distribution = null)
    {
        _distribution = distribution ?? "";
    }

    /// <inheritdoc/>
    public IPathMapper PathMapper => new WSLPathMapper();

    /// <inheritdoc/>
    public IExecutionContext CreateContext()
        => new NativeExecutionContext();

    /// <inheritdoc/>
    public void ApplyEnvironmentBinding(IExecutionContext context, EnvironmentBinding binding, ImplementationSelection implementation, string implementationPath)
    {
        if (implementation.ID.StartsWith(ExternalImplementation.PackagePrefix))
        {
            Log.Debug($"Skipping {binding} for {implementation}");
            return;
        }

        Log.Debug($"Applying {binding} for {implementation} in WSL");

        if (string.IsNullOrEmpty(binding.Name))
            throw new ExecutorException(string.Format(Resources.MissingBindingName, @"<environment>"));

        // Convert Windows paths to WSL paths
        string wslPath = ConvertToWSLPath(implementationPath);

        string newValue = binding switch
        {
            {Value: not null, Insert: not null} => throw new ExecutorException(Resources.EnvironmentBindingValueInvalid),
            {Value: not null} => binding.Value,
            _ => Path.Combine(wslPath, binding.Insert?.Replace('\\', '/') ?? "").Replace('\\', '/')
        };

        if (!context.ContainsEnvironmentVariable(binding.Name))
            context.SetEnvironmentVariable(binding.Name, binding.Default ?? "");

        string? previousValue = context.GetEnvironmentVariable(binding.Name);
        string separator = string.IsNullOrEmpty(binding.Separator) ? ":" : binding.Separator;

        string finalValue = binding.Mode switch
        {
            _ when string.IsNullOrEmpty(previousValue) => newValue,
            EnvironmentMode.Replace => newValue,
            EnvironmentMode.Prepend => newValue + separator + previousValue,
            EnvironmentMode.Append => previousValue + separator + newValue,
            _ => throw new InvalidOperationException($"Unknown {nameof(EnvironmentBinding)} value: {binding.Mode}")
        };

        context.SetEnvironmentVariable(binding.Name, finalValue);
    }

    /// <inheritdoc/>
    public string DeployExecutable(IExecutionContext context, string name, IEnumerable<string> commandLine)
    {
        // For WSL, create shell scripts
        string scriptPath = Path.Combine(
            Locations.GetCacheDirPath("0install.net", false, "injector", "wsl-executables", name),
            name);

        Directory.CreateDirectory(Path.GetDirectoryName(scriptPath)!);

        var commandLineList = commandLine.ToList();
        if (commandLineList is [])
            throw new ExecutorException(Resources.CommandLineEmpty);

        // Create a shell script
        var scriptContent = new List<string>
        {
            "#!/bin/bash",
            string.Join(" ", commandLineList.Select(arg => $"\"{arg}\"")) + " \"$@\""
        };
        File.WriteAllLines(scriptPath, scriptContent);

        // Return WSL path
        return ConvertToWSLPath(scriptPath);
    }

    /// <inheritdoc/>
    public void FinalizeExecution(IExecutionContext context, string mainExecutable, string arguments)
    {
        var nativeContext = (NativeExecutionContext)context;

        // Convert Windows path to WSL path
        string wslPath = ConvertToWSLPath(mainExecutable);

        // Build WSL command
        var wslArgs = new List<string>();

        if (!string.IsNullOrEmpty(_distribution))
        {
            wslArgs.Add("-d");
            wslArgs.Add(_distribution);
        }

        // Add environment variables
        foreach (var envVar in GetAllEnvironmentVariables(context))
        {
            wslArgs.Add($"{envVar.Key}={envVar.Value}");
        }

        wslArgs.Add(wslPath);
        // Note: arguments string is already escaped/quoted by ProcessStartInfo
        if (!string.IsNullOrEmpty(arguments))
            wslArgs.AddRange(arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        nativeContext.StartInfo.FileName = "wsl.exe";
        nativeContext.StartInfo.Arguments = string.Join(" ", wslArgs.Select(arg => $"\"{arg}\""));
    }

    /// <inheritdoc/>
    public Process Start(IExecutionContext context)
    {
        var startInfo = context.ToStartInfo();
        try
        {
            return startInfo.Start();
        }
        catch (IOException ex) when (!File.Exists(startInfo.FileName))
        {
            throw new FileNotFoundException(ex.Message + ": " + startInfo.FileName, startInfo.FileName);
        }
    }

    /// <summary>
    /// Converts a Windows path to a WSL path.
    /// </summary>
    private static string ConvertToWSLPath(string windowsPath)
    {
        // WSL maps C:\path to /mnt/c/path
        if (windowsPath.Length >= 2 && windowsPath[1] == ':')
        {
            char drive = char.ToLowerInvariant(windowsPath[0]);
            string path = windowsPath[2..].Replace('\\', '/');
            return $"/mnt/{drive}{path}";
        }
        return windowsPath.Replace('\\', '/');
    }

    private IEnumerable<KeyValuePair<string, string>> GetAllEnvironmentVariables(IExecutionContext context)
    {
        var nativeContext = (NativeExecutionContext)context;
#if NET
        return nativeContext.StartInfo.Environment.Where(x => x.Value != null)
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value!));
#else
        return nativeContext.StartInfo.EnvironmentVariables.Cast<System.Collections.DictionaryEntry>()
            .Select(x => new KeyValuePair<string, string>(x.Key.ToString()!, x.Value?.ToString() ?? ""));
#endif
    }

    private class WSLPathMapper : IPathMapper
    {
        public string MapPath(string hostPath)
            => ConvertToWSLPath(hostPath);

        public string UnmapPath(string targetPath)
        {
            if (targetPath.StartsWith("/mnt/"))
            {
                string[] parts = targetPath.Substring(5).Split(new[] { '/' }, 2);
                if (parts.Length >= 2 && parts[0].Length == 1 && char.IsLetter(parts[0][0]))
                    return $"{char.ToUpperInvariant(parts[0][0])}:\\{parts[1].Replace('/', '\\')}";
            }
            return targetPath;
        }
    }
}
