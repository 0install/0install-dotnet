// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Native;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Fluent-style builder for a process execution environment for a <see cref="Selections"/> document.
/// </summary>
public partial class EnvironmentBuilder(IImplementationStore implementationStore) : IEnvironmentBuilder
{
    /// <summary>
    /// Used to hold the process launch environment while it is being built.
    /// </summary>
    private readonly ProcessStartInfo _startInfo = new() {ErrorDialog = false, UseShellExecute = false};

#if NET
    private IDictionary<string, string?> EnvironmentVariables => _startInfo.Environment;
#else
    private System.Collections.Specialized.StringDictionary EnvironmentVariables => _startInfo.EnvironmentVariables;
#endif

    /// <summary>
    /// Used to hold the command-line of the main implementation while it is being built.
    /// </summary>
    private List<ArgBase>? _mainCommandLine;

    /// <summary>
    /// The set of <see cref="Implementation"/>s be injected into the execution environment.
    /// </summary>
    private Selections? _selections;

    /// <summary>
    /// Sets the <see cref="Selections"/> to be injected.
    /// Must be called before any methods of the <see cref="IEnvironmentBuilder"/> interface are used. May not be called more than once.
    /// </summary>
    /// <param name="selections">The set of <see cref="Implementation"/>s be injected into the execution environment.</param>
    /// <param name="overrideMain">An alternative executable to to run from the main <see cref="Implementation"/> instead of <see cref="Element.Main"/>. May not contain command-line arguments! Whitespaces do not need to be escaped.</param>
    /// <exception cref="ImplementationNotFoundException">One of the <see cref="Implementation"/>s is not cached yet.</exception>
    /// <exception cref="ExecutorException">The executor was unable to process the <see cref="Selections"/>.</exception>
    /// <exception cref="IOException">A problem occurred while writing a file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to a file is not permitted.</exception>
    /// <returns>The execution environment. Reference to self for fluent API use.</returns>
    public IEnvironmentBuilder Inject(Selections selections, string? overrideMain = null)
    {
        if (selections == null) throw new ArgumentNullException(nameof(selections));
        if (_selections != null) throw new InvalidOperationException($"{nameof(Inject)}() may not be called more than once.");

        if (string.IsNullOrEmpty(selections.Command)) throw new ExecutorException("The Selections document does not specify a start command.");
        if (selections.Implementations is []) throw new ExecutorException("The Selections document does not list any implementations.");
        _selections = selections;

        try
        {
            ApplyBindings();

            _mainCommandLine = GetCommandLine(GetMainImplementation(overrideMain), _selections.Command);
        }
        #region Error handling
        catch (KeyNotFoundException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new ExecutorException(ex.Message);
        }
        #endregion

        return this;
    }

    /// <inheritdoc/>
    public IEnvironmentBuilder AddWrapper(string? wrapper)
    {
        if (string.IsNullOrEmpty(wrapper)) return this;
        if (_selections == null) throw new InvalidOperationException($"{nameof(Inject)}() must be called first.");

        (string fileName, string arguments) = ProcessUtils.FromCommandLine(wrapper);
        _startInfo.FileName = fileName;
        _startInfo.Arguments = arguments;

        return this;
    }

    private readonly List<string> _userArguments = [];

    /// <inheritdoc/>
    public IEnvironmentBuilder AddArguments(params string[] arguments)
    {
        if (arguments == null) throw new ArgumentNullException(nameof(arguments));
        if (_selections == null) throw new InvalidOperationException($"{nameof(Inject)}() must be called first.");

        _userArguments.Add(arguments);

        return this;
    }

    /// <inheritdoc/>
    public IEnvironmentBuilder SetEnvironmentVariable(string name, string? value)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        if (_selections == null) throw new InvalidOperationException($"{nameof(Inject)}() must be called first.");

        EnvironmentVariables[name] = value ?? throw new ArgumentNullException(nameof(value));

        return this;
    }

    /// <inheritdoc/>
    public ProcessStartInfo ToStartInfo()
    {
        if (_selections == null || _mainCommandLine == null) throw new InvalidOperationException($"{nameof(Inject)}() must be called first.");

        try
        {
            ProcessRunEnvBindings();

            var args = ExpandCommandLine(_mainCommandLine);
            args.Add(_userArguments);

            if (string.IsNullOrEmpty(_startInfo.FileName))
            {
                (string fileName, string arguments) = SplitCommandLine(args);
                _startInfo.FileName = fileName;
                _startInfo.Arguments = arguments;
            }
            else
            {
                if (string.IsNullOrEmpty(_startInfo.Arguments))
                    _startInfo.Arguments = args.JoinEscapeArguments();
                else
                    _startInfo.Arguments += " " + args.JoinEscapeArguments();
            }
        }
        #region Error handling
        catch (KeyNotFoundException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new ExecutorException(ex.Message);
        }
        #endregion

        return _startInfo;
    }

    /// <inheritdoc/>
    public Process Start()
    {
        var startInfo = ToStartInfo();
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

    /// <summary>
    /// Returns the main (first) implementation of the selection.
    /// Replaces the <see cref="Command"/> of the main implementation with the binary specified in <paramref name="overrideMain"/> if set.
    /// </summary>
    /// <param name="overrideMain">An alternative executable to to run from the main <see cref="Implementation"/> instead of <see cref="Element.Main"/>. May not contain command-line arguments! Whitespaces do not need to be escaped.</param>
    private ImplementationSelection GetMainImplementation(string? overrideMain)
    {
        Debug.Assert(_selections != null);
        if (string.IsNullOrEmpty(overrideMain)) return _selections.MainImplementation;

        // Clone the first implementation so the command can replaced without affecting Selections
        var mainImplementation = ((ICloneable<ImplementationSelection>)_selections.MainImplementation).Clone();
        var command = mainImplementation[_selections.Command ?? Command.NameRun];
        Debug.Assert(command != null);

        string mainPath = overrideMain.ToNativePath();
        command.Path = (mainPath[0] == Path.DirectorySeparatorChar)
            // Relative to implementation root
            ? mainPath.TrimStart(Path.DirectorySeparatorChar)
            // Relative to original command
            : Path.Combine(Path.GetDirectoryName(command.Path) ?? "", mainPath);
        command.Arguments.Clear();

        return mainImplementation;
    }

    /// <summary>
    /// Determines the command-line needed to execute an <see cref="ImplementationSelection"/>. Recursively handles <see cref="Runner"/>s.
    /// </summary>
    /// <param name="implementation">The implementation to launch.</param>
    /// <param name="commandName">The name of the <see cref="Command"/> within the <paramref name="implementation"/> to launch.</param>
    /// <exception cref="KeyNotFoundException"><see cref="Selections"/> points to missing <see cref="Dependency"/>s.</exception>
    /// <exception cref="ImplementationNotFoundException">An <see cref="Implementation"/> is not cached yet.</exception>
    /// <exception cref="ExecutorException">A <see cref="Command"/> contained invalid data.</exception>
    /// <exception cref="IOException">A problem occurred while writing a file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to a file is not permitted.</exception>
    private List<ArgBase> GetCommandLine(ImplementationSelection implementation, string commandName)
    {
        Debug.Assert(_selections != null);
        if (implementation == null) throw new ArgumentNullException(nameof(implementation));
        if (commandName == null) throw new ArgumentNullException(nameof(commandName));
        if (commandName.Length == 0) throw new ExecutorException(string.Format(Resources.CommandNotSpecified, implementation.InterfaceUri));

        var command = implementation[commandName];
        Debug.Assert(command != null);

        // Apply bindings implementations use to find themselves and their dependencies
        ApplyBindings(command, implementation);
        if (command.WorkingDir != null) ApplyWorkingDir(command.WorkingDir, implementation);
        ApplyDependencyBindings(command);

        List<ArgBase> commandLine;
        if (command.Runner is {} runner)
        {
            commandLine = GetCommandLine(_selections[runner.InterfaceUri], runner.Command ?? Command.NameRun);
            commandLine.Add(runner.Arguments);
        }
        else
            commandLine = [];

        if (!string.IsNullOrEmpty(command.Path))
        {
            string path = command.Path.ToNativePath();
            if (!implementation.ID.StartsWith(ExternalImplementation.PackagePrefix))
                path = Path.Combine(implementationStore.GetPath(implementation), path);
            commandLine.Add(path);
        }
        commandLine.Add(command.Arguments);

        return commandLine;
    }

    /// <summary>
    /// Expands any Unix-style environment variables.
    /// </summary>
    /// <param name="commandLine">The command-line to expand.</param>
    private List<string> ExpandCommandLine(IEnumerable<ArgBase> commandLine)
    {
        var result = new List<string>();

        foreach (var part in commandLine)
        {
            switch (part)
            {
                case Arg arg:
                    result.Add(OSUtils.ExpandVariables(arg.Value, EnvironmentVariables));
                    break;

                case ForEachArgs forEach:
                    foreach (string value in EnvironmentVariables[forEach.ItemFrom]
                                          ?.Split(new[] {forEach.Separator ?? Path.PathSeparator.ToString(CultureInfo.InvariantCulture)}, StringSplitOptions.RemoveEmptyEntries)
                                         ?? [])
                    {
                        EnvironmentVariables["item"] = value;
                        result.Add(forEach.Arguments.Select(arg => OSUtils.ExpandVariables(arg.Value, EnvironmentVariables)));
                    }
                    EnvironmentVariables.Remove("item");
                    break;

                default:
                    throw new NotSupportedException($"Unknown command-line part: {part}");
            }
        }

        return result;
    }

    /// <summary>
    /// Splits a command-line into a file name and an arguments part.
    /// </summary>
    /// <param name="commandLine">The command-line to split.</param>
    private static (string fileName, string arguments) SplitCommandLine(IReadOnlyList<string> commandLine)
    {
        if (commandLine is []) throw new ExecutorException(Resources.CommandLineEmpty);
        return (commandLine[0], commandLine.Skip(1).JoinEscapeArguments());
    }
}
