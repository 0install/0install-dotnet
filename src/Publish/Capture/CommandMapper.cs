// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using NanoByte.Common;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.Capture
{
    /// <summary>
    /// Maps command-lines to the best matching <see cref="Command"/>.
    /// </summary>
    public class CommandMapper
    {
        /// <summary>
        /// An association of a command-line with a <see cref="Command"/>.
        /// </summary>
        private struct CommandTuple
        {
            /// <summary>The full command-line rooted in the filesystem.</summary>
            public readonly string CommandLine;

            /// <summary>The command coressponding to the execution of the command-line.</summary>
            public readonly Command Command;

            /// <summary>
            /// Creates a new command tuple.
            /// </summary>
            /// <param name="commandLine">The full command-line rooted in the filesystem.</param>
            /// <param name="command">The command coressponding to the execution of the command-line.</param>
            public CommandTuple(string commandLine, Command command)
            {
                CommandLine = commandLine;
                Command = command;
            }
        }

        /// <summary>A list of command-lines and coressponding <see cref="Command"/>s.</summary>
        private readonly List<CommandTuple> _commmands = new List<CommandTuple>();

        /// <summary>
        /// The fully qualified path to the installation directory.
        /// </summary>
        public string InstallationDir { get; }

        /// <summary>
        /// Creates a new command provider.
        /// </summary>
        /// <param name="installationDir">The fully qualified path to the installation directory.</param>
        /// <param name="commmands">A list of all known-commands available within the installation directory.</param>
        public CommandMapper([NotNull] string installationDir, [NotNull, ItemNotNull] IEnumerable<Command> commmands)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(installationDir)) throw new ArgumentNullException(nameof(installationDir));
            if (commmands == null) throw new ArgumentNullException(nameof(commmands));
            #endregion

            InstallationDir = installationDir;

            // Associate each command with its command-line
            foreach (var command in commmands)
            {
                string path = Path.Combine(installationDir, command.Path.Replace('/', Path.DirectorySeparatorChar));
                string arguments = command.Arguments.Select(arg => arg.ToString()).JoinEscapeArguments();

                _commmands.Add(GetCommandTuple(installationDir, command, escapePath: true));

                // Only add a version without escaping if it causes no ambiguities
                if (!path.ContainsWhitespace() || string.IsNullOrEmpty(arguments))
                    _commmands.Add(GetCommandTuple(installationDir, command, escapePath: false));
            }

            // Sort backwards to make sure the most specific matches are selected first
            _commmands.Sort((tuple1, tuple2) => string.CompareOrdinal(tuple2.CommandLine, tuple1.CommandLine));
        }

        private static CommandTuple GetCommandTuple(string installationDir, Command command, bool escapePath)
        {
            string path = Path.Combine(installationDir, command.Path.Replace('/', Path.DirectorySeparatorChar));
            string arguments = command.Arguments.Select(arg => arg.ToString()).JoinEscapeArguments();

            string commmandLine = escapePath ? ("\"" + path + "\"") : path;
            if (!string.IsNullOrEmpty(arguments)) commmandLine += " " + arguments;
            return new CommandTuple(commmandLine, command);
        }

        /// <summary>
        /// Tries to find the best-match <see cref="Command"/> for a command-line.
        /// </summary>
        /// <param name="commandLine">The fully qualified command-line to try to match.</param>
        /// <param name="additionalArgs">Any additional arguments from <paramref name="commandLine"/> that are not covered by the returned <see cref="Command"/>.</param>
        /// <returns>The best matching <see cref="Command"/> or <c>null</c> if no match was found.</returns>
        [CanBeNull]
        public Command GetCommand([NotNull] string commandLine, [CanBeNull] out string additionalArgs)
        {
            #region Sanity checks
            if (commandLine == null) throw new ArgumentNullException(nameof(commandLine));
            #endregion

            foreach (var tuple in _commmands.Where(tuple => commandLine.StartsWithIgnoreCase(tuple.CommandLine)))
            {
                additionalArgs = commandLine.Substring(tuple.CommandLine.Length).TrimStart();
                return tuple.Command;
            }

            // No match found
            additionalArgs = null;
            return null;
        }
    }
}
