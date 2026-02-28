// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Executes Linux binaries inside Docker containers.
/// </summary>
public class DockerStrategy : IExecutionStrategy
{
    private readonly string _image;
    private readonly List<(string hostPath, string containerPath)> _volumeMounts = [];

    /// <summary>
    /// Creates a new Docker execution strategy.
    /// </summary>
    /// <param name="image">The Docker image to use (e.g., "ubuntu:latest").</param>
    public DockerStrategy(string image = "ubuntu:latest")
    {
        _image = image;
    }

    /// <inheritdoc/>
    public IPathMapper PathMapper => new DockerPathMapper(this);

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

        Log.Debug($"Applying {binding} for {implementation} in Docker");

        if (string.IsNullOrEmpty(binding.Name))
            throw new ExecutorException(string.Format(Resources.MissingBindingName, @"<environment>"));

        // Map the implementation path to the container
        string containerPath = MapHostPathToContainer(implementationPath);
        _volumeMounts.Add((implementationPath, containerPath));

        string newValue = binding switch
        {
            {Value: not null, Insert: not null} => throw new ExecutorException(Resources.EnvironmentBindingValueInvalid),
            {Value: not null} => binding.Value,
            _ => Path.Combine(containerPath, binding.Insert.ToNativePath() ?? "").Replace('\\', '/')
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
        // For Docker, we create a wrapper script in the container
        string containerScriptPath = $"/tmp/0install-wrappers/{name}";
        string commandLineStr = string.Join(" ", commandLine.Select(arg => $"\"{arg}\""));

        context.SetEnvironmentVariable($"ZEROINSTALL_DOCKER_WRAPPER_{name}", commandLineStr);

        return containerScriptPath;
    }

    /// <inheritdoc/>
    public void FinalizeExecution(IExecutionContext context, string mainExecutable, string arguments)
    {
        var nativeContext = (NativeExecutionContext)context;

        // Build Docker command
        var dockerArgs = new List<string> { "run", "--rm" };

        // Add volume mounts
        foreach (var (hostPath, containerPath) in _volumeMounts.Distinct())
        {
            dockerArgs.Add("-v");
            dockerArgs.Add($"{hostPath}:{containerPath}:ro");
        }

        // Add environment variables
        foreach (var envVar in GetAllEnvironmentVariables(context))
        {
            dockerArgs.Add("-e");
            dockerArgs.Add($"{envVar.Key}={envVar.Value}");
        }

        // Add image and command
        dockerArgs.Add(_image);
        dockerArgs.Add(mainExecutable);
        // Note: arguments string is already escaped/quoted by ProcessStartInfo
        if (!string.IsNullOrEmpty(arguments))
            dockerArgs.AddRange(arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        nativeContext.StartInfo.FileName = "docker";
        nativeContext.StartInfo.Arguments = string.Join(" ", dockerArgs.Select(arg => $"\"{arg}\""));
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

    private string MapHostPathToContainer(string hostPath)
    {
        // Normalize and map host paths to container paths
        string normalizedPath = hostPath.Replace('\\', '/').TrimStart('/');
        return $"/0install/{normalizedPath}";
    }

    private IEnumerable<KeyValuePair<string, string>> GetAllEnvironmentVariables(IExecutionContext context)
    {
        // This is a workaround - ideally IExecutionContext would provide enumeration
        var nativeContext = (NativeExecutionContext)context;
#if NET
        return nativeContext.StartInfo.Environment.Where(x => x.Value != null)
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value!));
#else
        return nativeContext.StartInfo.EnvironmentVariables.Cast<System.Collections.DictionaryEntry>()
            .Select(x => new KeyValuePair<string, string>(x.Key.ToString()!, x.Value?.ToString() ?? ""));
#endif
    }

    private class DockerPathMapper(DockerStrategy strategy) : IPathMapper
    {
        public string MapPath(string hostPath)
            => strategy.MapHostPathToContainer(hostPath);

        public string UnmapPath(string targetPath)
            => throw new NotImplementedException("Docker path unmapping not yet implemented");
    }
}
