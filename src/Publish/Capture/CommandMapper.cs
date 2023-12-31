// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.Capture;

/// <summary>
/// Maps command-lines to the best matching <see cref="Command"/>.
/// </summary>
public class CommandMapper
{
    /// <summary>A list of command-lines and corresponding <see cref="Command"/>s.</summary>
    private readonly List<(string commandLine, Command command)> _commands = [];

    /// <summary>
    /// The fully qualified path to the installation directory.
    /// </summary>
    public string InstallationDir { get; }

    /// <summary>
    /// Creates a new command provider.
    /// </summary>
    /// <param name="installationDir">The fully qualified path to the installation directory.</param>
    /// <param name="commands">A list of all known-commands available within the installation directory.</param>
    public CommandMapper(string installationDir, [InstantHandle] IEnumerable<Command> commands)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(installationDir)) throw new ArgumentNullException(nameof(installationDir));
        if (commands == null) throw new ArgumentNullException(nameof(commands));
        #endregion

        InstallationDir = installationDir;

        // Associate each command with its command-line
        foreach (var command in commands)
        {
            if (string.IsNullOrEmpty(command.Path)) continue;

            string path = Path.Combine(installationDir, command.Path.Replace('/', Path.DirectorySeparatorChar));
            string arguments = command.Arguments.Select(arg => arg.ToString()!).JoinEscapeArguments();

            _commands.Add(((path.EscapeArgument() + " " + arguments).Trim(), command));

            // Only add a version without escaping if it causes no ambiguities
            if (!path.ContainsWhitespace() || string.IsNullOrEmpty(arguments))
                _commands.Add(((path + " " + arguments).Trim(), command));
        }

        // Sort backwards to make sure the most specific matches are selected first
        _commands.Sort((tuple1, tuple2) => string.CompareOrdinal(tuple2.commandLine, tuple1.commandLine));
    }

    /// <summary>
    /// Tries to find the best-match <see cref="Command"/> for a command-line.
    /// </summary>
    /// <param name="commandLine">The fully qualified command-line to try to match.</param>
    /// <param name="additionalArgs">Any additional arguments from <paramref name="commandLine"/> that are not covered by the returned <see cref="Command"/>.</param>
    /// <returns>The best matching <see cref="Command"/> or <c>null</c> if no match was found.</returns>
    public Command? GetCommand(string commandLine, out string? additionalArgs)
    {
        #region Sanity checks
        if (commandLine == null) throw new ArgumentNullException(nameof(commandLine));
        #endregion

        foreach ((string commandCommandLine, var command) in _commands)
        {
            if (commandLine.StartsWithIgnoreCase(commandCommandLine))
            {
                additionalArgs = commandLine[commandCommandLine.Length..].TrimStart();
                return command;
            }
        }

        // No match found
        additionalArgs = null;
        return null;
    }
}
