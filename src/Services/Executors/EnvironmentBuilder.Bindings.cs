// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using NanoByte.Common.Streams;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Executors;

public partial class EnvironmentBuilder
{
    /// <summary>
    /// Used to track <see cref="IBindingContainer"/> that have already been applied to avoid cycles.
    /// </summary>
    private readonly HashSet<IBindingContainer> _appliedBindingContainers = new();

    /// <summary>
    /// Applies all specified <see cref="Binding"/>s.
    /// </summary>
    /// <exception cref="KeyNotFoundException"><see cref="Selections"/> contains <see cref="Dependency"/>s pointing to interfaces without selections.</exception>
    /// <exception cref="ImplementationNotFoundException">An <see cref="Implementation"/> is not cached yet.</exception>
    /// <exception cref="ExecutorException">A <see cref="Command"/> contained invalid data.</exception>
    /// <exception cref="IOException">A problem occurred while writing a file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to a file is not permitted.</exception>
    private void ApplyBindings()
    {
        Debug.Assert(_selections != null);

        foreach (var implementation in _selections.Implementations)
        {
            // Self-bindings used by implementations to find their own resources
            ApplyBindings(implementation, implementation);

            ApplyDependencyBindings(implementation);
        }

        // Allow bindings to be reapplied for runners, etc.
        _appliedBindingContainers.Clear();
    }

    /// <summary>
    /// Applies <see cref="Binding"/>s to make a set of <see cref="Dependency"/>s available.
    /// </summary>
    /// <param name="dependencyContainer">The list of <see cref="Dependency"/>s to follow.</param>
    /// <exception cref="KeyNotFoundException"><see cref="Selections"/> contains <see cref="Dependency"/>s pointing to interfaces without selections.</exception>
    /// <exception cref="ImplementationNotFoundException">An <see cref="Implementation"/> is not cached yet.</exception>
    /// <exception cref="ExecutorException">A <see cref="Command"/> contained invalid data.</exception>
    /// <exception cref="IOException">A problem occurred while writing a file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to a file is not permitted.</exception>
    private void ApplyDependencyBindings(IDependencyContainer dependencyContainer)
    {
        Debug.Assert(_selections != null);

        foreach (var dependency in dependencyContainer.Dependencies.Where(x => x.Importance == Importance.Essential || _selections.ContainsImplementation(x.InterfaceUri)))
            ApplyBindings(dependency, _selections[dependency.InterfaceUri]);
    }

    /// <summary>
    /// Applies all <see cref="Binding"/>s listed in a specific <see cref="IBindingContainer"/>.
    /// </summary>
    /// <param name="bindingContainer">The list of <see cref="Binding"/>s to be performed.</param>
    /// <param name="implementation">The implementation to be made available via the <see cref="Binding"/>s.</param>
    /// <exception cref="ImplementationNotFoundException">The <paramref name="implementation"/> is not cached yet.</exception>
    /// <exception cref="ExecutorException">A <see cref="Command"/> contained invalid data.</exception>
    /// <exception cref="IOException">A problem occurred while writing a file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to a file is not permitted.</exception>
    private void ApplyBindings(IBindingContainer bindingContainer, ImplementationSelection implementation)
    {
        // Do not apply bindings more than once
        if (!_appliedBindingContainers.Add(bindingContainer)) return;

        if (bindingContainer.Bindings.Count == 0) return;

        // Don't use bindings for PackageImplementations
        if (implementation.ID.StartsWith(ExternalImplementation.PackagePrefix)) return;

        foreach (var binding in bindingContainer.Bindings)
        {
            switch (binding)
            {
                case EnvironmentBinding environmentBinding:
                    ApplyEnvironmentBinding(environmentBinding, implementation);
                    break;
                //case OverlayBinding overlayBinding: ApplyOverlayBinding(overlayBinding, implementation); break;
                case ExecutableInVar executableInVar:
                    ApplyExecutableInVar(executableInVar, implementation);
                    break;
                case ExecutableInPath executableInPath:
                    ApplyExecutableInPath(executableInPath, implementation);
                    break;
            }
        }
    }

    /// <summary>
    /// Applies an <see cref="EnvironmentBinding"/> by modifying environment variables.
    /// </summary>
    /// <param name="binding">The binding to apply.</param>
    /// <param name="implementation">The implementation to be made available.</param>
    /// <exception cref="ExecutorException"><see cref="EnvironmentBinding.Name"/> or other data is invalid.</exception>
    private void ApplyEnvironmentBinding(EnvironmentBinding binding, ImplementationSelection implementation)
    {
        Log.Debug("Applying " + binding + " for " + implementation);

        if (string.IsNullOrEmpty(binding.Name)) throw new ExecutorException(string.Format(Resources.MissingBindingName, @"<environment>"));

        var environmentVariables = _startInfo.EnvironmentVariables;

        string newValue;
        if (binding.Value != null && binding.Insert != null)
        { // Conflict
            throw new ExecutorException(Resources.EnvironmentBindingValueInvalid);
        }
        else if (binding.Value != null)
        { // Static value
            newValue = binding.Value;
        }
        else
        { // Path inside the implementation
            newValue = Path.Combine(_implementationStore.GetPath(implementation), binding.Insert.ToNativePath() ?? "");
        }

        // Set the default value if the variable is not already set on the system
        if (!environmentVariables.ContainsKey(binding.Name)) environmentVariables.Add(binding.Name, binding.Default);

        string? previousValue = environmentVariables[binding.Name];
        string separator = (string.IsNullOrEmpty(binding.Separator) ? Path.PathSeparator.ToString(CultureInfo.InvariantCulture) : binding.Separator);

        environmentVariables[binding.Name] = binding.Mode switch
        {
            _ when string.IsNullOrEmpty(previousValue) => newValue,
            EnvironmentMode.Replace => newValue,
            EnvironmentMode.Prepend => newValue + separator + environmentVariables[binding.Name],
            EnvironmentMode.Append => environmentVariables[binding.Name] + separator + newValue,
            _ => throw new InvalidOperationException($"Unknown {nameof(EnvironmentBinding)} value: {binding.Mode}")
        };
    }

    /// <summary>
    /// A list of run-environment executables pending to be configured.
    /// </summary>
    private readonly List<(string exeName, IEnumerable<ArgBase> commandLine)> _pendingRunEnvs = new();

    /// <summary>
    /// Applies an <see cref="ExecutableInVar"/> binding by creating a run-environment executable.
    /// </summary>
    /// <param name="binding">The binding to apply.</param>
    /// <param name="implementation">The implementation to be made available.</param>
    /// <exception cref="KeyNotFoundException"><see cref="Selections"/> points to missing <see cref="Dependency"/>s.</exception>
    /// <exception cref="ImplementationNotFoundException">One of the <see cref="Implementation"/>s is not cached yet.</exception>
    /// <exception cref="ExecutorException"><see cref="ExecutableInVar.Name"/> is invalid.</exception>
    /// <exception cref="IOException">A problem occurred while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
    private void ApplyExecutableInVar(ExecutableInVar binding, ImplementationSelection implementation)
    {
        Log.Debug("Applying " + binding + " for " + implementation);

        if (string.IsNullOrEmpty(binding.Name)) throw new ExecutorException(string.Format(Resources.MissingBindingName, @"<executable-in-var>"));
        if (Path.GetInvalidFileNameChars().Any(invalidChar => binding.Name.Contains(invalidChar.ToString(CultureInfo.InvariantCulture))))
            throw new ExecutorException(string.Format(Resources.IllegalCharInBindingName, @"<executable-in-var>"));

        string exePath = DeployRunEnvExecutable(binding.Name);

        // Point variable directly to executable
        if (_startInfo.EnvironmentVariables.ContainsKey(binding.Name)) Log.Warn("Overwriting existing environment variable with <executable-in-var>: " + binding.Name);
        _startInfo.EnvironmentVariables[binding.Name] = exePath;

        // Tell the executable what command-line to run
        _pendingRunEnvs.Add((binding.Name, GetCommandLine(implementation, binding.Command ?? Command.NameRun)));
    }

    /// <summary>
    /// Applies an <see cref="ExecutableInPath"/> binding by creating a run-environment executable.
    /// </summary>
    /// <param name="binding">The binding to apply.</param>
    /// <param name="implementation">The implementation to be made available.</param>
    /// <exception cref="KeyNotFoundException"><see cref="Selections"/> points to missing <see cref="Dependency"/>s.</exception>
    /// <exception cref="ImplementationNotFoundException">One of the <see cref="Implementation"/>s is not cached yet.</exception>
    /// <exception cref="ExecutorException"><see cref="ExecutableInPath.Name"/> is invalid.</exception>
    /// <exception cref="IOException">A problem occurred while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
    private void ApplyExecutableInPath(ExecutableInPath binding, ImplementationSelection implementation)
    {
        Log.Debug("Applying " + binding + " for " + implementation);

        if (string.IsNullOrEmpty(binding.Name)) throw new ExecutorException(string.Format(Resources.MissingBindingName, @"<executable-in-path>"));
        if (Path.GetInvalidFileNameChars().Any(invalidChar => binding.Name.Contains(invalidChar.ToString(CultureInfo.InvariantCulture))))
            throw new ExecutorException(string.Format(Resources.IllegalCharInBindingName, @"<executable-in-path>"));

        string exePath = DeployRunEnvExecutable(binding.Name);

        // Add executable directory to PATH variable
        _startInfo.EnvironmentVariables[WindowsUtils.IsWindows ? "Path" : "PATH"] =
            Path.GetDirectoryName(exePath) + Path.PathSeparator +
            _startInfo.EnvironmentVariables[WindowsUtils.IsWindows ? "Path" : "PATH"];

        // Tell the executable what command-line to run
        _pendingRunEnvs.Add((binding.Name, GetCommandLine(implementation, binding.Command ?? Command.NameRun)));
    }

    /// <summary>
    /// Deploys a copy (hard or soft link if possible) of the run-environment executable within a cache directory.
    /// </summary>
    /// <param name="name">The executable name to deploy under (without file extensions).</param>
    /// <returns>The fully qualified path of the deployed run-environment executable.</returns>
    /// <exception cref="IOException">A problem occurred while writing the file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
    /// <remarks>A run-environment executable executes a command-line specified in an environment variable based on its own name.</remarks>
    private static string DeployRunEnvExecutable(string name)
    {
        string templatePath = GetRunEnvTemplate();
        string deployedPath = Path.Combine(Locations.GetCacheDirPath("0install.net", false, "injector", "executables", name), name);
        if (WindowsUtils.IsWindows) deployedPath += ".exe";

        Log.Info("Deploying run-environment executable to: " + deployedPath);
        try
        {
            if (File.Exists(deployedPath)) File.Delete(deployedPath);
            FileUtils.CreateHardlink(deployedPath, templatePath);
        }
        #region Error handling
        catch (IOException) when (File.Exists(deployedPath))
        {
            // File already exists and was probably locked because it was in use
        }
        catch (UnauthorizedAccessException) when (File.Exists(deployedPath))
        {
            // File already exists and was probably locked because it was in use
        }
        catch (PlatformNotSupportedException)
        {
            // Unable to hardlink, fallback to simple copy
            File.Copy(templatePath, deployedPath);
        }
        #endregion

        if (UnixUtils.IsUnix) FileUtils.SetExecutable(deployedPath, true);

        return deployedPath;
    }

    /// <summary>
    /// Deploys an appropriate runenv binary template for the current operating system.
    /// </summary>
    /// <returns>The path to the deployed executable file.</returns>
    private static string GetRunEnvTemplate()
    {
        string templateName = WindowsUtils.IsWindows
            ? (Environment.Version.Major == 4)
                ? "runenv.clr4.template"
                : "runenv.clr2.template"
            : "runenv.sh.template";

        string path = Path.Combine(Locations.GetCacheDirPath("0install.net", false, "injector", "executables"), templateName);
        Log.Info("Writing run-environment template to: " + path);
        try
        {
            typeof(Executor).CopyEmbeddedToFile(templateName, path);
        }
        #region Error handling
        catch (IOException ex)
        {
            Log.Info("Unable to write run-environment template.");
            Log.Info(ex);
        }
        #endregion

        return path;
    }

    /// <summary>
    /// Split and apply command-lines for executable bindings.
    /// This is delayed until the end because environment variables that might be modified are expanded.
    /// </summary>
    private void ProcessRunEnvBindings()
    {
        foreach (var (exeName, args) in _pendingRunEnvs)
        {
            var commandLine = ExpandCommandLine(args);
            if (WindowsUtils.IsWindows)
            {
                var (fileName, arguments) = SplitCommandLine(commandLine);
                _startInfo.EnvironmentVariables["ZEROINSTALL_RUNENV_FILE_" + exeName] = fileName;
                _startInfo.EnvironmentVariables["ZEROINSTALL_RUNENV_ARGS_" + exeName] = arguments;
            }
            else _startInfo.EnvironmentVariables["ZEROINSTALL_RUNENV_" + exeName] = commandLine.JoinEscapeArguments();
        }
        _pendingRunEnvs.Clear();
    }

    /// <summary>
    /// Applies a <see cref="WorkingDir"/> change to the <see cref="ProcessStartInfo"/>.
    /// </summary>
    /// <param name="binding">The <see cref="WorkingDir"/> to apply.</param>
    /// <param name="implementation">The implementation to be made available via the <see cref="WorkingDir"/> change.</param>
    /// <exception cref="ImplementationNotFoundException">The <paramref name="implementation"/> is not cached yet.</exception>
    /// <exception cref="ExecutorException">The <paramref name="binding"/> has an invalid path or another working directory has already been set.</exception>
    private void ApplyWorkingDir(WorkingDir binding, ImplementationSelection implementation)
    {
        Log.Debug("Applying " + binding + " for " + implementation);

        string source = binding.Source.ToNativePath() ?? "";
        if (Path.IsPathRooted(source) || source.Contains(".." + Path.DirectorySeparatorChar)) throw new ExecutorException(Resources.WorkingDirInvalidPath);

        // Only allow working directory to be changed once
        if (!string.IsNullOrEmpty(_startInfo.WorkingDirectory)) throw new ExecutorException(Resources.Working);

        _startInfo.WorkingDirectory = Path.Combine(_implementationStore.GetPath(implementation), source);
    }
}
