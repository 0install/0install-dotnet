// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NDesk.Options;
using ZeroInstall.Commands.Properties;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Common base class for commands that provide multiple sub-<see cref="CliCommand"/>s.
    /// </summary>
    public abstract class CliMultiCommand : CliCommand
    {
        #region Metadata
        /// <inheritdoc/>
        public override string Description
        {
            get
            {
                var builder = new StringBuilder(Resources.TryHelpWith + Environment.NewLine);
                foreach (string possibleSubCommand in SubCommandNames)
                    builder.AppendLine("0install " + FullName + " " + possibleSubCommand);
                return builder.ToString();
            }
        }

        /// <inheritdoc/>
        public override string Usage => "SUBCOMMAND";

        /// <inheritdoc/>
        protected override int AdditionalArgsMin => 1;
        #endregion

        /// <inheritdoc/>
        protected CliMultiCommand(ICommandHandler handler)
            : base(handler)
        {
            // Defer all option parsing to the sub-commands
            Options.Clear();
        }

        /// <summary>
        /// A list of sub-command names (without alternatives) as used in command-line arguments in lower-case.
        /// </summary>
        public abstract IEnumerable<string> SubCommandNames { get; }

        /// <summary>
        /// Creates a new sub-<see cref="CliCommand"/> based on a name.
        /// </summary>
        /// <param name="commandName">The command name to look for; case-insensitive.</param>
        /// <returns>The requested sub-<see cref="CliCommand"/>.</returns>
        /// <exception cref="OptionException"><paramref name="commandName"/> is an unknown command.</exception>
        /// <exception cref="IOException">There was a problem accessing a configuration file or one of the stores.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to a configuration file or one of the stores was not permitted.</exception>
        /// <exception cref="InvalidDataException">A configuration file is damaged.</exception>
        public abstract CliCommand GetCommand(string commandName);

        /// <summary>The sub-command selected in <see cref="Parse"/> and used in <see cref="Execute"/>.</summary>
        private CliCommand? _subCommand;

        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public override void Parse(IEnumerable<string> args)
        {
            base.Parse(args);

            string? subCommandName = GetCommandName(ref args);
            if (subCommandName == null) return;

            _subCommand = GetCommand(subCommandName);
            _subCommand.Parse(args);
        }

        /// <inheritdoc/>
        public override ExitCode Execute()
        {
            if (_subCommand == null)
            {
                Handler.Output(Resources.CommandLineArguments, HelpText);
                return ExitCode.UserCanceled;
            }

            return _subCommand.Execute();
        }
    }
}
