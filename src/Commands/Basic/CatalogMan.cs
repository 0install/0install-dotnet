// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// Manages the <see cref="Catalog"/>s provided by the <see cref="ICatalogManager"/>.
/// </summary>
public sealed partial class CatalogMan(ICommandHandler handler) : CliMultiCommand(handler)
{
    public const string Name = "catalog";

    /// <inheritdoc/>
    public override IEnumerable<string> SubCommandNames => [Search.Name, Refresh.Name, Add.Name, Remove.Name, Reset.Name, List.Name];

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

    private abstract class CatalogSubCommand(ICommandHandler handler) : CliCommand(handler), ICliSubCommand
    {
        public string ParentName => Name;
    }
}
