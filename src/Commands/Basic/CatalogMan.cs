// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Model;
using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Basic
{
    /// <summary>
    /// Manages the <see cref="Catalog"/>s provided by the <see cref="ICatalogManager"/>.
    /// </summary>
    public sealed partial class CatalogMan : CliMultiCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "catalog";

        /// <inheritdoc/>
        public CatalogMan(ICommandHandler handler)
            : base(handler)
        {}
        #endregion

        /// <inheritdoc/>
        public override IEnumerable<string> SubCommandNames => new[] {Search.Name, Refresh.Name, Add.Name, Remove.Name, Reset.Name, List.Name};

        /// <inheritdoc/>
        public override CliCommand GetCommand(string commandName)
            => (commandName ?? throw new ArgumentNullException(nameof(commandName))) switch
            {
                Search.Name => new Search(Handler),
                Refresh.Name => new Refresh(Handler),
                Add.Name => new Add(Handler),
                Remove.Name => new Remove(Handler),
                Reset.Name => new Reset(Handler),
                List.Name => new List(Handler),
                _ => throw new OptionException(string.Format(Resources.UnknownCommand, commandName), commandName)
            };

        private abstract class CatalogSubCommand : CliCommand, ICliSubCommand
        {
            public string ParentName => Name;

            protected CatalogSubCommand(ICommandHandler handler)
                : base(handler)
            {}
        }
    }
}
