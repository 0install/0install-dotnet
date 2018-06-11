// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using JetBrains.Annotations;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Common base class for sub-commands that are aggregated by a <see cref="MultiCommandBase"/>.
    /// </summary>
    public abstract class SubCommandBase : CommandBase
    {
        /// <summary>
        /// The <see cref="CommandBase.Name"/> of the <see cref="MultiCommandBase"/> this command is a sub-command of.
        /// </summary>
        [NotNull]
        protected abstract string ParentName { get; }

        /// <inheritdoc/>
        [NotNull]
        public override string Name => ParentName + " " + base.Name;

        /// <inheritdoc/>
        protected SubCommandBase([NotNull] ICommandHandler handler)
            : base(handler)
        {}
    }
}
