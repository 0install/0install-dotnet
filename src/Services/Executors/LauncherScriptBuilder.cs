// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using System.Text.RegularExpressions;
using ZeroInstall.Model.Selection;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Generates scripts that launch a <see cref="Selections"/> document from a directory containing copies of its implementations.
/// </summary>
/// <param name="implementationStore">Not used for locating implementations. Required by <see cref="EnvironmentBuilder"/>.</param>
/// <param name="getRelativePath">Returns the path of an implementation's directory relative to the directory the launcher script is placed in.</param>
internal abstract partial class LauncherScriptBuilder(IImplementationStore implementationStore, Func<ImplementationSelection, string> getRelativePath)
    : EnvironmentBuilder(implementationStore)
{
    /// <summary>
    /// Creates a builder for scripts that run on a specific operating system.
    /// </summary>
    /// <param name="os">The operating system the scripts will run on. Generates PowerShell scripts for <see cref="OS.Windows"/> and POSIX shell scripts otherwise.</param>
    /// <param name="implementationStore">Not used for locating implementations. Required by <see cref="EnvironmentBuilder"/>.</param>
    /// <param name="getRelativePath">Returns the path of an implementation's directory relative to the directory the launcher script is placed in.</param>
    public static LauncherScriptBuilder For(OS os, IImplementationStore implementationStore, Func<ImplementationSelection, string> getRelativePath)
        => os == OS.Windows
            ? new PowerShellLauncherScriptBuilder(implementationStore, getRelativePath)
            : new PosixLauncherScriptBuilder(implementationStore, getRelativePath);

    /// <summary>
    /// The name of the directory holding executables for <see cref="ExecutableInVar"/> and <see cref="ExecutableInPath"/> bindings, relative to the launcher script.
    /// </summary>
    public const string BinDir = "bin";

    /// <summary>
    /// Stands in for the directory containing the launcher script in paths, until they are converted to script code.
    /// </summary>
    private protected const string RootMarker = "__0INSTALL_LAUNCHER_ROOT__";

    /// <summary>
    /// The file extension to append to the name of the launcher script, including the leading dot. Empty if none.
    /// </summary>
    public abstract string LauncherExtension { get; }

    /// <summary>
    /// The encoding to use when writing the launcher script to a file.
    /// </summary>
    public abstract Encoding Encoding { get; }

    /// <summary>
    /// The character separating directories in paths on the target system.
    /// </summary>
    private protected abstract char DirectorySeparator { get; }

    /// <inheritdoc/>
    private protected override string GetPathInImplementation(ImplementationSelection implementation, string? relativePath)
    {
        string path = RootMarker + getRelativePath(implementation);
        return string.IsNullOrEmpty(relativePath) ? path : path + DirectorySeparator + relativePath.Replace('/', DirectorySeparator);
    }

    /// <inheritdoc/>
    private protected override string GetPathInPackage(string path)
        => path.Replace('/', DirectorySeparator); // Uses the separator of the target system rather than the system generating the script

    /// <summary>
    /// Generates the main launcher script. Must be placed in the directory that implementation paths are relative to.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="EnvironmentBuilder.Inject"/> has not been called yet.</exception>
    /// <exception cref="ExecutorException">The command-line is empty or refers to variables in an unsupported way.</exception>
    public abstract string GetLauncher();

    /// <summary>
    /// Generates executables for <see cref="ExecutableInVar"/> and <see cref="ExecutableInPath"/> bindings. Must be placed in <see cref="BinDir"/>.
    /// </summary>
    /// <returns>Pairs of file names and file contents.</returns>
    /// <exception cref="InvalidOperationException"><see cref="EnvironmentBuilder.Inject"/> has not been called yet.</exception>
    /// <exception cref="ExecutorException">A command-line is empty or refers to variables in an unsupported way.</exception>
    public abstract IEnumerable<(string name, byte[] content)> GetExecutables();

    /// <summary>
    /// Generates a wrapper that runs the main launcher script, for systems where scripts like it cannot be started directly.
    /// Must be placed next to the main launcher script, with the same name except for the file extension.
    /// </summary>
    /// <returns>The file extension for the wrapper, including the leading dot, and its contents. <c>null</c> if no wrapper is needed.</returns>
    public virtual (string extension, byte[] content)? GetWrapper() => null;

    /// <summary>
    /// Returns the command-line of the main implementation.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="EnvironmentBuilder.Inject"/> has not been called yet.</exception>
    private protected IReadOnlyList<ArgBase> RequireMainCommandLine()
        => MainCommandLine ?? throw new InvalidOperationException($"{nameof(Inject)}() must be called first.");

    /// <summary>
    /// A part of an <see cref="Arg"/> value.
    /// </summary>
    private protected abstract record ArgPart;

    /// <summary>
    /// Text to be used as is, with no variables being expanded.
    /// </summary>
    private protected sealed record LiteralPart(string Value) : ArgPart;

    /// <summary>
    /// A reference to an environment variable.
    /// </summary>
    /// <param name="Name">The name of the variable.</param>
    /// <param name="Operator"><c>:-</c> to use <paramref name="Default"/> if the variable is unset or empty, <c>-</c> to use it if the variable is unset, <c>null</c> for no default.</param>
    /// <param name="Default">The default value. May contain <see cref="LiteralPart"/>s and <see cref="VariablePart"/>s without defaults of their own.</param>
    private protected sealed record VariablePart(string Name, string? Operator = null, IReadOnlyList<ArgPart>? Default = null) : ArgPart;

    /// <summary>
    /// Splits an <see cref="Arg"/> value into literal text and variable references, matching <c>OSUtils.ExpandVariables()</c>.
    /// References to variables with names that cannot be used on the target system are replaced with their default values.
    /// </summary>
    private protected IEnumerable<ArgPart> ParseArg(string value)
    {
        int position = 0;
        foreach (Match match in LongVariableRegex().Matches(value))
        {
            foreach (var part in ParseShortVariables(value.Substring(position, match.Index - position)))
                yield return part;
            position = match.Index + match.Length;

            if (match.Index >= 1 && value[match.Index - 1] == '$')
            {
                // Treat $$ as escaping (the preceding $ was already returned as a literal)
                yield return new LiteralPart("{" + match.Groups[1].Value + "}");
                continue;
            }

            string expression = match.Groups[1].Value;
            (string name, string? op, string? defaultValue) = expression.IndexOf(":-", StringComparison.Ordinal) is var colonDash and >= 0
                ? (expression[..colonDash], ":-", expression[(colonDash + 2)..])
                : expression.IndexOf('-') is var dash and >= 0
                    ? (expression[..dash], "-", expression[(dash + 1)..])
                    : (expression, null, null);

            // OSUtils.ExpandVariables() expands short-style variables after long-style ones, which includes those in default values
            var defaultParts = defaultValue == null ? null : ParseShortVariables(defaultValue).ToList();

            if (IsValidVariableName(name))
                yield return new VariablePart(name, op, defaultParts);
            else
            {
                // Variables with names that are not valid on the target system cannot be set, so only the default value applies
                Log.Warn($"The variable name '{name}' is not supported in launcher scripts. Using its default value instead.");
                foreach (var part in defaultParts ?? [])
                    yield return part;
            }
        }
        foreach (var part in ParseShortVariables(value[position..]))
            yield return part;
    }

    /// <summary>
    /// Splits a string that may contain short-style variables (<c>$NAME</c>) into literal text and variable references.
    /// </summary>
    private IEnumerable<ArgPart> ParseShortVariables(string value)
    {
        int position = 0;
        foreach (Match match in ShortVariableRegex().Matches(value))
        {
            yield return new LiteralPart(value.Substring(position, match.Index - position));
            position = match.Index + match.Length;

            string name = match.Groups[1].Value;
            if (match.Index >= 1 && value[match.Index - 1] == '$')
                yield return new LiteralPart(name); // Treat $$ as escaping (the preceding $ was already returned as a literal)
            else if (IsValidVariableName(name))
                yield return new VariablePart(name);
            else
                Log.Warn($"The variable name '{name}' is not supported in launcher scripts. Using an empty string instead.");
        }
        yield return new LiteralPart(value[position..]);
    }

    /// <summary>
    /// Determines whether a variable name can be used in scripts on the target system.
    /// </summary>
    private protected abstract bool IsValidVariableName(string? name);

    /// <summary>
    /// Ensures a variable name can be used in scripts on the target system.
    /// </summary>
    /// <exception cref="ExecutorException"><paramref name="name"/> is not a valid variable name.</exception>
    private protected string RequireVariableName(string? name, string element)
        => IsValidVariableName(name)
            ? name!
            : throw new ExecutorException($"The variable name '{name}' in {element} is not supported in launcher scripts.");

    // Must match OSUtils.ExpandVariables()
    private const string
        LongVariablePattern = @"\${([^{}]+)}",
        ShortVariablePattern = @"\$([^{}\$\s\\/-]+)";

#if NET
    [GeneratedRegex(LongVariablePattern)]
    private static partial Regex LongVariableRegex();
    [GeneratedRegex(ShortVariablePattern)]
    private static partial Regex ShortVariableRegex();
#else
    private static readonly Regex
        _longVariableRegex = new(LongVariablePattern),
        _shortVariableRegex = new(ShortVariablePattern);
    private static Regex LongVariableRegex() => _longVariableRegex;
    private static Regex ShortVariableRegex() => _shortVariableRegex;
#endif
}
