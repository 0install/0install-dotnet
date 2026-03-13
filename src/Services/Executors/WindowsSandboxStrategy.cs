// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Executes Windows binaries in Windows Sandbox for isolated execution.
/// </summary>
public class WindowsSandboxStrategy : IExecutionStrategy
{
    private readonly List<(string hostPath, string sandboxPath)> _mappedFolders = [];
    private readonly string _sandboxConfigPath;

    /// <summary>
    /// Creates a new Windows Sandbox execution strategy.
    /// </summary>
    public WindowsSandboxStrategy()
    {
        _sandboxConfigPath = Path.Combine(
            Path.GetTempPath(),
            $"0install-sandbox-{Guid.NewGuid()}.wsb");
    }

    /// <inheritdoc/>
    public IPathMapper PathMapper => new SandboxPathMapper(this);

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

        Log.Debug($"Applying {binding} for {implementation} in Windows Sandbox");

        if (string.IsNullOrEmpty(binding.Name))
            throw new ExecutorException(string.Format(Resources.MissingBindingName, @"<environment>"));

        // Map the implementation directory to the sandbox
        string sandboxPath = MapHostPathToSandbox(implementationPath);
        _mappedFolders.Add((implementationPath, sandboxPath));

        string newValue = binding switch
        {
            {Value: not null, Insert: not null} => throw new ExecutorException(Resources.EnvironmentBindingValueInvalid),
            {Value: not null} => binding.Value,
            _ => Path.Combine(sandboxPath, binding.Insert ?? "")
        };

        if (!context.ContainsEnvironmentVariable(binding.Name))
            context.SetEnvironmentVariable(binding.Name, binding.Default ?? "");

        string? previousValue = context.GetEnvironmentVariable(binding.Name);
        string separator = string.IsNullOrEmpty(binding.Separator) ? ";" : binding.Separator;

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
        // For Windows Sandbox, create batch scripts
        string scriptPath = Path.Combine(
            Locations.GetCacheDirPath("0install.net", false, "injector", "sandbox-executables", name),
            name + ".bat");

        Directory.CreateDirectory(Path.GetDirectoryName(scriptPath)!);

        var commandLineList = commandLine.ToList();
        if (commandLineList is [])
            throw new ExecutorException(Resources.CommandLineEmpty);

        // Create a batch file
        string batchContent = "@echo off\r\n" + string.Join(" ", commandLineList.Select(arg => $"\"{arg}\"")) + " %*\r\n";
        File.WriteAllText(scriptPath, batchContent);

        // Map this directory to the sandbox
        string scriptDir = Path.GetDirectoryName(scriptPath)!;
        string sandboxScriptDir = MapHostPathToSandbox(scriptDir);
        _mappedFolders.Add((scriptDir, sandboxScriptDir));

        return Path.Combine(sandboxScriptDir, Path.GetFileName(scriptPath));
    }

    /// <inheritdoc/>
    public void FinalizeExecution(IExecutionContext context, string mainExecutable, string arguments)
    {
        var nativeContext = (NativeExecutionContext)context;

        // Create a startup script that sets environment variables and runs the main executable
        string startupScriptPath = Path.Combine(Path.GetTempPath(), $"0install-sandbox-startup-{Guid.NewGuid()}.cmd");
        var scriptLines = new List<string> { "@echo off" };

        // Add environment variables
        foreach (var envVar in GetAllEnvironmentVariables(context))
        {
            scriptLines.Add($"set {envVar.Key}={envVar.Value}");
        }

        // Add the main command
        scriptLines.Add($"\"{mainExecutable}\" {arguments}");
        scriptLines.Add("pause");

        File.WriteAllLines(startupScriptPath, scriptLines);

        // Create Windows Sandbox configuration
        CreateSandboxConfig(startupScriptPath);

        // Launch Windows Sandbox with the configuration
        nativeContext.StartInfo.FileName = "WindowsSandbox.exe";
        nativeContext.StartInfo.Arguments = $"\"{_sandboxConfigPath}\"";
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

    private void CreateSandboxConfig(string startupScriptPath)
    {
        // Create a Windows Sandbox configuration XML
        var config = new System.Text.StringBuilder();
        config.AppendLine("<Configuration>");
        config.AppendLine("  <MappedFolders>");

        // Add the startup script folder
        string startupScriptDir = Path.GetDirectoryName(startupScriptPath)!;
        config.AppendLine("    <MappedFolder>");
        config.AppendLine($"      <HostFolder>{startupScriptDir}</HostFolder>");
        config.AppendLine($"      <SandboxFolder>C:\\Startup</SandboxFolder>");
        config.AppendLine("      <ReadOnly>true</ReadOnly>");
        config.AppendLine("    </MappedFolder>");

        // Add implementation folders
        foreach (var (hostPath, sandboxPath) in _mappedFolders.Distinct())
        {
            config.AppendLine("    <MappedFolder>");
            config.AppendLine($"      <HostFolder>{hostPath}</HostFolder>");
            config.AppendLine($"      <SandboxFolder>{sandboxPath}</SandboxFolder>");
            config.AppendLine("      <ReadOnly>true</ReadOnly>");
            config.AppendLine("    </MappedFolder>");
        }

        config.AppendLine("  </MappedFolders>");
        config.AppendLine("  <LogonCommand>");
        config.AppendLine($"    <Command>C:\\Startup\\{Path.GetFileName(startupScriptPath)}</Command>");
        config.AppendLine("  </LogonCommand>");
        config.AppendLine("</Configuration>");

        File.WriteAllText(_sandboxConfigPath, config.ToString());
        Log.Info($"Created Windows Sandbox configuration: {_sandboxConfigPath}");
    }

    private string MapHostPathToSandbox(string hostPath)
    {
        // Use a deterministic hash to avoid collisions
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hostPath));
        string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()[..16];
        return $"C:\\0install\\impl_{hash}";
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

    private class SandboxPathMapper(WindowsSandboxStrategy strategy) : IPathMapper
    {
        public string MapPath(string hostPath)
            => strategy.MapHostPathToSandbox(hostPath);

        public string UnmapPath(string targetPath)
            => throw new NotImplementedException("Windows Sandbox path unmapping not yet implemented");
    }
}
