// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Common base class for sub-commands that are aggregated by a <see cref="CliMultiCommand"/>.
    /// </summary>
    public abstract class CliSubCommand : CliCommand
    {
        /// <summary>
        /// The <see cref="CliCommand.Name"/> of the <see cref="CliMultiCommand"/> this command is a sub-command of.
        /// </summary>
        protected abstract string ParentName { get; }

        /// <inheritdoc/>
        public override string Name => ParentName + " " + base.Name;

        /// <inheritdoc/>
        protected CliSubCommand(ICommandHandler handler)
            : base(handler)
        {}
    }
}
