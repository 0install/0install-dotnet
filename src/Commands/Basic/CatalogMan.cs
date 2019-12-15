// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Commands.Basic
{
    /// <summary>
    /// Manages the <see cref="Catalog"/>s provided by the <see cref="ICatalogManager"/>.
    /// </summary>
    public sealed partial class CatalogMan : CliMultiCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public new const string Name = "catalog";

        /// <inheritdoc/>
        public CatalogMan([NotNull] ICommandHandler handler)
            : base(handler)
        {}
        #endregion

        /// <inheritdoc/>
        public override IEnumerable<string> SubCommandNames => new[] {Search.Name, Refresh.Name, Add.Name, Remove.Name, Reset.Name, List.Name};

        /// <inheritdoc/>
        public override CliSubCommand GetCommand(string commandName)
            => (commandName ?? throw new ArgumentNullException(nameof(commandName))) switch
            {
                Search.Name => (CliSubCommand)new Search(Handler),
                Refresh.Name => new Refresh(Handler),
                Add.Name => new Add(Handler),
                Remove.Name => new Remove(Handler),
                Reset.Name => new Reset(Handler),
                List.Name => new List(Handler),
                _ => throw new OptionException(string.Format(Resources.UnknownCommand, commandName), commandName)
            };

        private abstract class CatalogSubCommand : CliSubCommand
        {
            protected override string ParentName => CatalogMan.Name;

            protected CatalogSubCommand([NotNull] ICommandHandler handler)
                : base(handler)
            {}
        }
    }
}
