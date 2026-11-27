// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Generates command-lines for running Zero Install applications, for use in the <c>Exec</c> key of Desktop Entries and similar files.
/// </summary>
internal static class CommandLine
{
    /// <summary>
    /// Placeholder for the file or URL to be opened.
    /// </summary>
    public const string Item = "${item}";

    /// <summary>
    /// Returns the path of the Zero Install executable to use in generated files.
    /// </summary>
    /// <param name="gui"><c>true</c> to prefer <c>0install-gui</c>; <c>false</c> to use <c>0install</c>.</param>
    public static string GetLauncher(bool gui = true)
    {
        if (gui)
        {
            string guiPath = Paths.Combine(Locations.InstallBase, "0install-gui");
            if (File.Exists(guiPath)) return guiPath;
        }
        return Paths.Combine(Locations.InstallBase, "0install");
    }

    /// <summary>
    /// Returns the arguments to pass to <c>0install</c> to run an application.
    /// </summary>
    /// <param name="uri">The URI of the application to run.</param>
    /// <param name="command">The command within the application to run; can be <c>null</c>.</param>
    public static IEnumerable<string> GetRunArguments(FeedUri uri, string? command)
    {
        yield return "run";
        if (!string.IsNullOrEmpty(command))
        {
            yield return "--command";
            yield return command;
        }
        yield return uri.ToStringRfc();
    }

    /// <summary>
    /// Returns the arguments to pass to <c>0install</c> to run a verb.
    /// </summary>
    /// <param name="uri">The URI of the application to run.</param>
    /// <param name="verb">The verb to run.</param>
    /// <returns>The arguments, with <see cref="Item"/> in place of the file or URL to be opened.</returns>
    public static IReadOnlyList<string> GetRunArguments(FeedUri uri, Verb verb)
        => [..GetRunArguments(uri, verb.Command), ..GetVerbArguments(verb)];

    private static IEnumerable<string> GetVerbArguments(Verb verb)
    {
        if (verb.Arguments is not []) return verb.Arguments.Select(x => x.Value);
        if (string.IsNullOrEmpty(verb.ArgumentsLiteral)) return [Item];

        return ProcessUtils.SplitArguments(verb.ArgumentsLiteral)
                           .Where(x => x != "%*") // Additional arguments, only used on Windows
                           .Select(x => x.Replace("%V", Item).Replace("%1", Item).Replace("%L", Item));
    }

    /// <summary>
    /// Formats a command-line for use in the <c>Exec</c> key of a Desktop Entry or Nemo action.
    /// </summary>
    /// <param name="arguments">The executable and its arguments.</param>
    /// <param name="fieldCode">The field code to replace <see cref="Item"/> with; <c>null</c> to not replace it.</param>
    public static string ToExec(this IEnumerable<string> arguments, string? fieldCode = null)
    {
        var args = arguments.ToList();
        if (fieldCode == null) return string.Join(" ", args.Select(Escape));

        if (args.CountItems() > 1) args = WrapInShell(args); // There must not be more than one field code
        return string.Join(" ", args.Select(arg => ReplaceItem(arg, fieldCode, Escape))); // Field codes must not be inside quotes
    }

    /// <summary>
    /// Counts how often <see cref="Item"/> is used.
    /// </summary>
    public static int CountItems(this IEnumerable<string> arguments)
        => arguments.Sum(x => Split(x).Length - 1);

    /// <summary>
    /// Wraps a command-line in a shell, so that <see cref="Item"/> is only used once.
    /// </summary>
    private static List<string> WrapInShell(IEnumerable<string> arguments)
        => ["/bin/sh", "-c", "exec " + string.Join(" ", arguments.Select(arg => ReplaceItem(arg, "\"$1\"", x => x.EscapeArgumentPosix()))), "sh", Item];

    /// <summary>
    /// Replaces <see cref="Item"/> in an argument while escaping everything else.
    /// </summary>
    private static string ReplaceItem(string argument, string replacement, Func<string, string> escape)
        => Split(argument) is {Length: > 1} parts
            ? string.Join(replacement, parts.Select(x => x.Length == 0 ? "" : escape(x)))
            : escape(argument);

    private static string[] Split(string argument)
        => argument.Split([Item], StringSplitOptions.None);

    /// <summary>
    /// Escapes a string for use as an argument in an <c>Exec</c> key.
    /// </summary>
    /// <remarks>The Desktop Entry Specification uses the same rules for quoting as POSIX shells use for <c>"</c>.</remarks>
    private static string Escape(string value)
        => value.Replace("%", "%%").EscapeArgumentPosix();
}
