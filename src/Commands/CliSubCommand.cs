// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using JetBrains.Annotations;

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
        [NotNull]
        protected abstract string ParentName { get; }

        /// <inheritdoc/>
        [NotNull]
        public override string Name => ParentName + " " + base.Name;

        /// <inheritdoc/>
        protected CliSubCommand([NotNull] ICommandHandler handler)
            : base(handler)
        {}
    }
}
