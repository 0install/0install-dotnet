// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using System.Text.RegularExpressions;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Generates POSIX shell scripts that launch a <see cref="Selections"/> document from a directory containing copies of its implementations.
/// </summary>
/// <param name="implementationStore">Not used for locating implementations. Required by <see cref="EnvironmentBuilder"/>.</param>
/// <param name="getRelativePath">Returns the path of an implementation's directory relative to the directory the launcher script is placed in.</param>
internal sealed partial class PosixLauncherScriptBuilder(IImplementationStore implementationStore, Func<ImplementationSelection, string> getRelativePath)
    : LauncherScriptBuilder(implementationStore, getRelativePath)
{
    /// <summary>
    /// Shell variable holding the absolute path of the directory containing the launcher script.
    /// </summary>
    private const string DirVar = "_zeroinstall_dir";

    /// <summary>
    /// The default separator for lists of paths in environment variables on POSIX systems.
    /// </summary>
    private const string PathListSeparator = ":";

    private readonly List<string> _environmentStatements = [];

    /// <inheritdoc/>
    public override string LauncherExtension => "";

    /// <inheritdoc/>
    public override Encoding Encoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <inheritdoc/>
    private protected override char DirectorySeparator => '/';

    /// <inheritdoc/>
    private protected override void ApplyEnvironmentValue(EnvironmentBinding binding, string newValue)
    {
        string name = RequireVariableName(binding.Name, "<environment>");
        string separator = string.IsNullOrEmpty(binding.Separator) ? PathListSeparator : binding.Separator;

        // Set the default value if the variable is not already set on the system
        if (binding.Default != null)
            _environmentStatements.Add($"if [ -z \"${{{name}+x}}\" ]; then {name}={QuoteLiteral(binding.Default)}; fi");

        _environmentStatements.Add(binding.Mode switch
        {
            EnvironmentMode.Replace => $"export {name}={QuoteLiteral(newValue)}",
            EnvironmentMode.Prepend => $"if [ -n \"${{{name}}}\" ]; then export {name}={QuoteLiteral(newValue + separator)}\"${{{name}}}\"; else export {name}={QuoteLiteral(newValue)}; fi",
            EnvironmentMode.Append => $"if [ -n \"${{{name}}}\" ]; then export {name}=\"${{{name}}}\"{QuoteLiteral(separator + newValue)}; else export {name}={QuoteLiteral(newValue)}; fi",
            _ => throw new InvalidOperationException($"Unknown {nameof(EnvironmentBinding)} value: {binding.Mode}")
        });
    }

    /// <inheritdoc/>
    private protected override string DeployExecutable(string name)
        => $"{RootMarker}{BinDir}/{name}";

    /// <inheritdoc/>
    private protected override void SetExecutableVariable(string name, string exePath)
        => _environmentStatements.Add($"export {RequireVariableName(name, "<executable-in-var>")}={QuoteLiteral(exePath)}");

    /// <inheritdoc/>
    private protected override void AddExecutableToSearchPath(string exePath)
        => _environmentStatements.Add($"export PATH={QuoteLiteral(RootMarker + BinDir)}\"${{PATH:+:$PATH}}\"");

    /// <inheritdoc/>
    public override string GetLauncher()
    {
        var commandLine = RequireMainCommandLine();

        var script = new StringBuilder();
        AppendHeader(script);
        foreach (string statement in _environmentStatements)
            script.Append(statement).Append('\n');
        AppendRunEnvs(script);
        if (WorkingDirectory is {} workingDirectory)
            script.Append($"cd {QuoteLiteral(workingDirectory)} || exit\n");
        AppendExec(script, commandLine);
        return script.ToString();
    }

    /// <inheritdoc/>
    public override IEnumerable<(string name, byte[] content)> GetExecutables()
    {
        RequireMainCommandLine();

        // Runs the command-line the main launcher stored in an environment variable, like the run-environment script used by Executor
        foreach ((string name, _) in PendingRunEnvs)
            yield return (name, Encoding.GetBytes($"{Preamble}eval \"exec ${GetRunEnvVarName(name)} \\\"\\$@\\\"\"\n"));
    }

    /// <summary>
    /// Appends code that stores the command-lines for run-environment executables in environment variables, as single strings with shell quoting.
    /// Runs after all bindings have been applied, so that variables are expanded the same way as by <see cref="Executor"/>.
    /// </summary>
    private void AppendRunEnvs(StringBuilder script)
    {
        if (!PendingRunEnvs.Any()) return;

        // Pure shell code, since the bindings may have changed PATH
        script.Append("""
            _zeroinstall_quote() {
                _zeroinstall_rest=$1
                _zeroinstall_cmd=$_zeroinstall_cmd\'
                while :; do
                    case $_zeroinstall_rest in
                        *\'*)
                            _zeroinstall_head=${_zeroinstall_rest%%\'*}
                            _zeroinstall_cmd=$_zeroinstall_cmd$_zeroinstall_head\'\\\'\'
                            _zeroinstall_rest=${_zeroinstall_rest#*\'} ;;
                        *) break ;;
                    esac
                done
                _zeroinstall_cmd="$_zeroinstall_cmd$_zeroinstall_rest' "
            }

            """.Replace("\r\n", "\n"));

        foreach ((string name, var commandLine) in PendingRunEnvs)
        {
            var list = commandLine.ToList();
            if (list is []) throw new ExecutorException(Resources.CommandLineEmpty);

            // Uses a function to get a separate set of positional parameters
            script.Append("_zeroinstall_runenv() {\n");
            script.Append("set --\n");
            AppendArgs(script, list);
            script.Append($$"""
                _zeroinstall_cmd=
                for _zeroinstall_arg in "$@"; do _zeroinstall_quote "$_zeroinstall_arg"; done
                export {{GetRunEnvVarName(name)}}="$_zeroinstall_cmd"
                }
                _zeroinstall_runenv

                """.Replace("\r\n", "\n"));
        }
        script.Append("unset _zeroinstall_cmd _zeroinstall_arg _zeroinstall_rest _zeroinstall_head\n");
    }

    private const string Preamble = "#!/bin/sh\n# Generated by Zero Install. Changes will be overwritten when the directory is updated.\n";

    /// <summary>
    /// Appends code that determines the absolute path of the directory containing the script, following symlinks to the script.
    /// </summary>
    /// <remarks>
    /// Uses parameter expansion instead of <c>dirname</c>, so <c>readlink</c> is the only external utility required.
    /// Runs before any bindings are applied, so utilities are looked up in the caller's <c>PATH</c>.
    /// Clears <c>CDPATH</c> for <c>cd</c>, which could otherwise print the directory or switch to a different one with the same relative path.
    /// </remarks>
    private static void AppendHeader(StringBuilder script)
        => script.Append(Preamble).Append($$"""
            _zeroinstall_self=$0
            while [ -L "$_zeroinstall_self" ]; do
                _zeroinstall_link=$(readlink "$_zeroinstall_self")
                case $_zeroinstall_link in
                    /*) _zeroinstall_self=$_zeroinstall_link ;;
                    *) case $_zeroinstall_self in
                        */*) _zeroinstall_self=${_zeroinstall_self%/*}/$_zeroinstall_link ;;
                        *) _zeroinstall_self=$_zeroinstall_link ;;
                    esac ;;
                esac
            done
            case $_zeroinstall_self in
                */*) _zeroinstall_self=${_zeroinstall_self%/*} ;;
                *) _zeroinstall_self=. ;;
            esac
            {{DirVar}}=$(CDPATH= cd -- "$_zeroinstall_self/." && pwd -P) || exit
            unset _zeroinstall_self _zeroinstall_link

            """.Replace("\r\n", "\n"));

    /// <summary>
    /// Appends code that replaces the shell with the specified command-line, followed by the arguments passed to the script.
    /// </summary>
    private void AppendExec(StringBuilder script, IReadOnlyList<ArgBase> commandLine)
    {
        if (commandLine is []) throw new ExecutorException(Resources.CommandLineEmpty);

        if (!commandLine.OfType<ForEachArgs>().Any())
        {
            script.Append("exec");
            foreach (var arg in commandLine.Cast<Arg>())
                script.Append(' ').Append(QuoteArg(arg.Value));
            script.Append(" \"$@\"\n");
            return;
        }

        // Build the command-line behind the script's own arguments, then rotate those to the end
        script.Append("_zeroinstall_argc=$#\n");
        AppendArgs(script, commandLine);
        script.Append("""
            while [ "$_zeroinstall_argc" -gt 0 ]; do set -- "$@" "$1"; shift; _zeroinstall_argc=$((_zeroinstall_argc - 1)); done
            exec "$@"

            """.Replace("\r\n", "\n"));
    }

    /// <summary>
    /// Appends code that adds the specified command-line to the end of the positional parameters.
    /// </summary>
    /// <exception cref="ExecutorException">The command-line cannot be represented in a shell script.</exception>
    private void AppendArgs(StringBuilder script, IReadOnlyList<ArgBase> commandLine)
    {
        foreach (var part in commandLine)
        {
            switch (part)
            {
                case Arg arg:
                    script.Append($"set -- \"$@\" {QuoteArg(arg.Value)}\n");
                    break;

                case ForEachArgs forEach:
                    string itemFrom = RequireVariableName(forEach.ItemFrom, "<for-each>");
                    string separator = forEach.Separator ?? PathListSeparator;

                    if (separator.Length > 1)
                        throw new ExecutorException($"The separator '{separator}' in <for-each> is not supported in launcher scripts, since it is longer than one character.");

                    script.Append($"set -f; IFS={separator.EscapeArgumentPosix()}\n");
                    script.Append($"for item in ${{{itemFrom}}}; do\n");
                    script.Append("    [ -n \"$item\" ] || continue\n");
                    script.Append("    set -- \"$@\"");
                    foreach (var arg in forEach.Arguments)
                        script.Append(' ').Append(QuoteArg(arg.Value));
                    script.Append('\n');
                    script.Append("done\n");
                    script.Append("unset IFS item; set +f\n");
                    break;

                default:
                    throw new NotSupportedException($"Unknown command-line part: {part}");
            }
        }
    }

    /// <summary>
    /// Shell code referring to the directory containing the launcher script, followed by a directory separator.
    /// </summary>
    private const string DirReference = $"\"${{{DirVar}}}\"/";

    /// <summary>
    /// Converts a string into a single shell word, with no variables being expanded.
    /// Replaces <see cref="LauncherScriptBuilder.RootMarker"/> with a reference to the directory containing the launcher script.
    /// </summary>
    private static string QuoteLiteral(string value)
        => value.Length == 0
            ? value.EscapeArgumentPosix()
            : string.Join(DirReference, value.Split([RootMarker], StringSplitOptions.None).Select(part => part.Length == 0 ? "" : part.EscapeArgumentPosix()));

    /// <summary>
    /// Converts an <see cref="Arg"/> value into a single shell word,
    /// with environment variables expanded when the script runs, matching <c>OSUtils.ExpandVariables()</c>.
    /// </summary>
    /// <exception cref="ExecutorException">A variable default value contains characters that cannot be represented safely.</exception>
    private string QuoteArg(string value)
    {
        var result = new StringBuilder();
        foreach (var part in ParseArg(value))
        {
            switch (part)
            {
                case LiteralPart {Value.Length: > 0} literal:
                    result.Append(QuoteLiteral(literal.Value));
                    break;

                case VariablePart {Operator: null} variable:
                    result.Append($"\"${{{variable.Name}}}\"");
                    break;

                case VariablePart variable:
                    result.Append($"\"${{{variable.Name}{variable.Operator}");
                    foreach (var defaultPart in variable.Default!)
                    {
                        switch (defaultPart)
                        {
                            case LiteralPart literal:
                                // Quote characters inside ${...} are handled inconsistently by different shells (e.g., ' in Bash's POSIX mode), so only allow defaults that need no escaping
                                if (literal.Value.IndexOfAny(['\\', '"', '\'', '`', '$']) >= 0 || literal.Value.Contains(RootMarker))
                                    throw new ExecutorException($"Default value for variable '{variable.Name}' contains characters that are not supported in launcher scripts: {literal.Value}");
                                result.Append(literal.Value);
                                break;

                            case VariablePart nested:
                                result.Append($"${{{nested.Name}}}");
                                break;
                        }
                    }
                    result.Append("}\"");
                    break;
            }
        }

        // An empty word would vanish instead of being passed on as an empty argument
        return result.Length == 0 ? "".EscapeArgumentPosix() : result.ToString();
    }

    /// <inheritdoc/>
    private protected override bool IsValidVariableName(string? name)
        => !string.IsNullOrEmpty(name) && ShellNameRegex().IsMatch(name);

    private const string ShellNamePattern = "^[A-Za-z_][A-Za-z0-9_]*$";

#if NET
    [GeneratedRegex(ShellNamePattern)]
    private static partial Regex ShellNameRegex();
#else
    private static readonly Regex _shellNameRegex = new(ShellNamePattern);
    private static Regex ShellNameRegex() => _shellNameRegex;
#endif
}
