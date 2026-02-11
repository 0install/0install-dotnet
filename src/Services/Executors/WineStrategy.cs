// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Executes Windows binaries on Linux using Wine.
/// </summary>
public class WineStrategy : IExecutionStrategy
{
    private readonly string _winePrefix;

    /// <summary>
    /// Creates a new Wine execution strategy.
    /// </summary>
    /// <param name="winePrefix">The Wine prefix directory to use. If null, uses the default Wine prefix.</param>
    public WineStrategy(string? winePrefix = null)
    {
        _winePrefix = winePrefix ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".wine");
    }

    /// <inheritdoc/>
    public IPathMapper PathMapper => new WinePathMapper();

    /// <inheritdoc/>
    public IExecutionContext CreateContext()
    {
        var context = new NativeExecutionContext();
        // Set Wine prefix environment variable
        context.SetEnvironmentVariable("WINEPREFIX", _winePrefix);
        return context;
    }

    /// <inheritdoc/>
    public void ApplyEnvironmentBinding(IExecutionContext context, EnvironmentBinding binding, ImplementationSelection implementation, string implementationPath)
    {
        if (implementation.ID.StartsWith(ExternalImplementation.PackagePrefix))
        {
            Log.Debug($"Skipping {binding} for {implementation}");
            return;
        }

        Log.Debug($"Applying {binding} for {implementation} in Wine");

        if (string.IsNullOrEmpty(binding.Name))
            throw new ExecutorException(string.Format(Resources.MissingBindingName, @"<environment>"));

        // Convert Linux paths to Wine-compatible Windows paths
        string winePath = ConvertToWinePath(implementationPath);

        string newValue = binding switch
        {
            {Value: not null, Insert: not null} => throw new ExecutorException(Resources.EnvironmentBindingValueInvalid),
            {Value: not null} => binding.Value,
            _ => Path.Combine(winePath, binding.Insert ?? "").Replace('/', '\\')
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
        // For Wine, create Windows-style batch scripts
        string scriptPath = Path.Combine(
            Locations.GetCacheDirPath("0install.net", false, "injector", "wine-executables", name),
            name + ".bat");

        Directory.CreateDirectory(Path.GetDirectoryName(scriptPath)!);

        var commandLineList = commandLine.ToList();
        if (commandLineList is [])
            throw new ExecutorException(Resources.CommandLineEmpty);

        // Create a simple batch file
        string batchContent = "@echo off\r\n" + string.Join(" ", commandLineList.Select(arg => $"\"{arg}\"")) + " %*\r\n";
        File.WriteAllText(scriptPath, batchContent);

        // Wine will execute this batch file
        return ConvertToWinePath(scriptPath);
    }

    /// <inheritdoc/>
    public void FinalizeExecution(IExecutionContext context, string mainExecutable, string arguments)
    {
        var nativeContext = (NativeExecutionContext)context;

        // Convert the main executable path to Wine path if it's a Windows executable
        string winePath = mainExecutable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? ConvertToWinePath(mainExecutable)
            : mainExecutable;

        // Use wine or wine64 to execute
        nativeContext.StartInfo.FileName = "wine";
        nativeContext.StartInfo.Arguments = $"\"{winePath}\" {arguments}";
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
    /// Converts a Linux path to a Wine-compatible Windows path.
    /// </summary>
    private static string ConvertToWinePath(string linuxPath)
    {
        // Wine maps /home to Z:\home, etc.
        if (linuxPath.StartsWith("/"))
            return "Z:" + linuxPath.Replace('/', '\\');
        return linuxPath;
    }

    private class WinePathMapper : IPathMapper
    {
        public string MapPath(string hostPath)
            => ConvertToWinePath(hostPath);

        public string UnmapPath(string targetPath)
        {
            if (targetPath.StartsWith("Z:\\", StringComparison.OrdinalIgnoreCase))
                return targetPath[2..].Replace('\\', '/');
            throw new NotImplementedException("Wine path unmapping for drive letters not yet implemented");
        }
    }
}
