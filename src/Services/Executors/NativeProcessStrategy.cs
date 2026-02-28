// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using NanoByte.Common.Streams;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Executes processes directly on the native operating system without virtualization.
/// </summary>
public class NativeProcessStrategy : IExecutionStrategy
{
    /// <inheritdoc/>
    public IPathMapper PathMapper { get; } = new NativePathMapper();

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

        Log.Debug($"Applying {binding} for {implementation}");

        if (string.IsNullOrEmpty(binding.Name)) throw new ExecutorException(string.Format(Resources.MissingBindingName, @"<environment>"));

        string newValue = binding switch
        {
            // Conflict
            {Value: not null, Insert: not null} => throw new ExecutorException(Resources.EnvironmentBindingValueInvalid),
            // Static value
            {Value: not null} => binding.Value,
            // Path inside the implementation
            _ => Path.Combine(implementationPath, binding.Insert.ToNativePath() ?? "")
        };

        // Set the default value if the variable is not already set on the system
        if (!context.ContainsEnvironmentVariable(binding.Name))
            context.SetEnvironmentVariable(binding.Name, binding.Default ?? "");

        string? previousValue = context.GetEnvironmentVariable(binding.Name);
        string separator = string.IsNullOrEmpty(binding.Separator) ? Path.PathSeparator.ToString(CultureInfo.InvariantCulture) : binding.Separator;

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
        string templatePath = GetRunEnvTemplate();
        string path = Path.Combine(Locations.GetCacheDirPath("0install.net", false, "injector", "executables", name), name);
        if (WindowsUtils.IsWindows) path += ".exe";

        Log.Info($"Deploying run-environment executable to: {path}");
        try
        {
            if (File.Exists(path)) File.Delete(path);
            FileUtils.CreateHardlink(path, templatePath);
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && File.Exists(path))
        {
            Log.Debug($"Unable to update already existing '{path}'. Probably locked because it is in use.", ex);
        }
        catch (PlatformNotSupportedException)
        {
            // Unable to hardlink, fallback to simple copy
            File.Copy(templatePath, path);
        }
        #endregion

        if (UnixUtils.IsUnix) FileUtils.SetExecutable(path, true);

        // Store command-line in environment variables for the executable to use
        var commandLineList = commandLine.ToList();
        if (WindowsUtils.IsWindows)
        {
            if (commandLineList is [])
                throw new ExecutorException(Resources.CommandLineEmpty);

            string fileName = commandLineList[0];
            string arguments = commandLineList.Skip(1).JoinEscapeArguments();
            context.SetEnvironmentVariable($"ZEROINSTALL_RUNENV_FILE_{name}", fileName);
            context.SetEnvironmentVariable($"ZEROINSTALL_RUNENV_ARGS_{name}", arguments);
        }
        else
        {
            context.SetEnvironmentVariable($"ZEROINSTALL_RUNENV_{name}", commandLineList.JoinEscapeArguments());
        }

        return path;
    }

    /// <summary>
    /// Deploys an appropriate runenv binary template for the current operating system.
    /// </summary>
    /// <returns>The path to the deployed executable file.</returns>
    private static string GetRunEnvTemplate()
    {
        string path = Path.Combine(Locations.GetCacheDirPath("0install.net", false, "injector", "executables"), WindowsUtils.IsWindows
            ? "runenv.exe.template"
            : "runenv.sh.template");

        Log.Info($"Writing run-environment template to: {path}");
        try
        {
            if (WindowsUtils.IsWindows)
                typeof(Executor).CopyEmbeddedToFile("runenv.exe.template", path);
            else
                File.WriteAllLines(path, ["#!/bin/bash", "env_var_name=ZEROINSTALL_RUNENV_$(basename $0)", "${!env_var_name} \"$@\""]);
        }
        #region Error handling
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && File.Exists(path))
        {
            Log.Debug($"Unable to update already existing '{path}'. Probably locked because it is in use.", ex);
        }
        #endregion

        return path;
    }

    /// <inheritdoc/>
    public void FinalizeExecution(IExecutionContext context, string mainExecutable, string arguments)
    {
        var nativeContext = (NativeExecutionContext)context;
        nativeContext.StartInfo.FileName = mainExecutable;
        nativeContext.StartInfo.Arguments = arguments;
    }

    /// <inheritdoc/>
    public Process Start(IExecutionContext context)
    {
        var startInfo = context.ToStartInfo();
        try
        {
            return startInfo.Start();
        }
        #region Error handling
        catch (IOException ex) when (!File.Exists(startInfo.FileName))
        {
            // Replace with more specialized exception type
            throw new FileNotFoundException(ex.Message + ": " + startInfo.FileName, startInfo.FileName);
        }
        #endregion
    }
}
