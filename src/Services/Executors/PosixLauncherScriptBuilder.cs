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
        AppendHeader(script, relativeRoot: ".");
        foreach (string statement in _environmentStatements)
            script.Append(statement).Append('\n');
        if (WorkingDirectory is {} workingDirectory)
            script.Append($"cd {QuoteLiteral(workingDirectory)} || exit\n");
        AppendExec(script, commandLine);
        return script.ToString();
    }

    /// <inheritdoc/>
    public override IEnumerable<(string name, byte[] content)> GetExecutables()
    {
        RequireMainCommandLine();

        foreach ((string name, var commandLine) in PendingRunEnvs)
        {
            var script = new StringBuilder();
            AppendHeader(script, relativeRoot: "..");
            AppendExec(script, commandLine.ToList());
            yield return (name, Encoding.GetBytes(script.ToString()));
        }
    }

    /// <summary>
    /// Appends code that determines the absolute path of the root directory, following symlinks to the script.
    /// </summary>
    /// <param name="script">The script to append to.</param>
    /// <param name="relativeRoot">The path of the root directory relative to the directory containing the script.</param>
    private static void AppendHeader(StringBuilder script, string relativeRoot)
        => script.Append($"""
            #!/bin/sh
            # Generated by Zero Install. Changes will be overwritten when the directory is updated.
            _zeroinstall_self=$0
            while [ -L "$_zeroinstall_self" ]; do
                _zeroinstall_link=$(readlink "$_zeroinstall_self")
                case $_zeroinstall_link in
                    /*) _zeroinstall_self=$_zeroinstall_link ;;
                    *) _zeroinstall_self=$(dirname "$_zeroinstall_self")/$_zeroinstall_link ;;
                esac
            done
            {DirVar}=$(cd "$(dirname "$_zeroinstall_self")/{relativeRoot}" && pwd -P) || exit

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
        script.Append("""
            while [ "$_zeroinstall_argc" -gt 0 ]; do set -- "$@" "$1"; shift; _zeroinstall_argc=$((_zeroinstall_argc - 1)); done
            exec "$@"

            """.Replace("\r\n", "\n"));
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
                    // Quote characters inside ${...} are handled inconsistently by different shells, so only allow defaults that need no escaping
                    if (variable.Default!.IndexOfAny(['\\', '"', '`', '$']) >= 0 || variable.Default.Contains(RootMarker))
                        throw new ExecutorException($"Default value for variable '{variable.Name}' contains characters that are not supported in launcher scripts: {variable.Default}");
                    result.Append($"\"${{{variable.Name}{variable.Operator}{variable.Default}}}\"");
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
