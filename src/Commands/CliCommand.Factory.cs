// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NDesk.Options;
using ZeroInstall.Commands.Basic;
using ZeroInstall.Commands.Desktop;
using ZeroInstall.Commands.Properties;

namespace ZeroInstall.Commands;

partial class CliCommand
{
    /// <summary>
    /// A list of command names (without alternatives) as used in command-line arguments in lower-case.
    /// </summary>
    internal static readonly string[] Names =
    {
        Selection.Name, Download.Name, Update.Name, Run.Name, Import.Name, Export.Name, Search.Name, List.Name, CatalogMan.Name, Configure.Name, TrustMan.Name, AddFeed.Name, RemoveFeed.Name, ListFeeds.Name, Digest.Name, StoreMan.Name,
        Central.Name, AddApp.Name, RemoveApp.Name, RemoveAllApps.Name, IntegrateApp.Name, AddAlias.Name, ListApps.Name, UpdateApps.Name, RepairApps.Name, SyncApps.Name, ImportApps.Name, Self.Name
    };

    /// <summary>
    /// Creates a new <see cref="CliCommand"/> based on a name.
    /// </summary>
    /// <param name="commandName">The command name to look for; case-insensitive; can be <c>null</c>.</param>
    /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
    /// <returns>The requested <see cref="CliCommand"/> or <see cref="DefaultCommand"/> if <paramref name="commandName"/> was <c>null</c>.</returns>
    /// <exception cref="OptionException"><paramref name="commandName"/> is an unknown command.</exception>
    /// <exception cref="IOException">There was a problem accessing a configuration file or one of the stores.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to a configuration file or one of the stores was not permitted.</exception>
    /// <exception cref="InvalidDataException">A configuration file is damaged.</exception>
    public static CliCommand Create(string? commandName, ICommandHandler handler)
        => (commandName ?? "").ToLowerInvariant() switch
        {
            "" => new DefaultCommand(handler),
            ExportHelp.Name => new ExportHelp(handler),
            Selection.Name => new Selection(handler),
            Download.Name => new Download(handler),
            Fetch.Name => new Fetch(handler),
            Update.Name => new Update(handler),
            Run.Name => new Run(handler),
            Import.Name => new Import(handler),
            Export.Name => new Export(handler),
            Search.Name => new Search(handler),
            List.Name => new List(handler),
            CatalogMan.Name => new CatalogMan(handler),
            Configure.Name => new Configure(handler),
            TrustMan.Name => new TrustMan(handler),
            AddFeed.Name => new AddFeed(handler),
            RemoveFeed.Name => new RemoveFeed(handler),
            ListFeeds.Name => new ListFeeds(handler),
            Digest.Name => new Digest(handler),
            StoreMan.Name => new StoreMan(handler),
            Central.Name => new Central(handler),
            AddApp.Name or AddApp.AltName => new AddApp(handler),
            RemoveApp.Name or RemoveApp.AltName or RemoveApp.AltName2 => new RemoveApp(handler),
            RemoveAllApps.Name or RemoveAllApps.AltName => new RemoveAllApps(handler),
            IntegrateApp.Name or IntegrateApp.AltName or IntegrateApp.AltName2 => new IntegrateApp(handler),
            AddAlias.Name or AddAlias.AltName => new AddAlias(handler),
            ListApps.Name => new ListApps(handler),
            UpdateApps.Name or UpdateApps.AltName => new UpdateApps(handler),
            RepairApps.Name or RepairApps.AltName => new RepairApps(handler),
            ImportApps.Name => new ImportApps(handler),
            SyncApps.Name => new SyncApps(handler),
            Self.Name or Self.AltName => new Self(handler),
            Self.Update.TopLevelName => new Self.Update(handler),
            _ => throw new OptionException(string.Format(Resources.UnknownCommand, commandName), commandName)
        };

    /// <summary>
    /// Parses command-line arguments, automatically creating an appropriate <see cref="CliCommand"/>.
    /// </summary>
    /// <param name="args">The command-line arguments to be parsed.</param>
    /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
    /// <returns>The newly created <see cref="CliCommand"/> after <see cref="CliCommand.Parse"/> has been called.</returns>
    /// <exception cref="OperationCanceledException">The user asked to see help information, version information, etc..</exception>
    /// <exception cref="OptionException"><paramref name="args"/> contains unknown options or specified an unknown command.</exception>
    /// <exception cref="IOException">A problem occurred while creating a directory.</exception>
    /// <exception cref="UnauthorizedAccessException">Creating a directory is not permitted.</exception>
    /// <exception cref="InvalidDataException">A configuration file is damaged.</exception>
    /// <exception cref="FormatException">An URI, local path, version number, etc. is invalid.</exception>
    public static CliCommand CreateAndParse(IReadOnlyList<string> args, ICommandHandler handler)
    {
        #region Sanity checks
        if (args == null) throw new ArgumentNullException(nameof(args));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        #endregion

        var command = Create(GetCommandName(ref args), handler);
        command.Parse(args);
        return command;
    }

    /// <summary>
    /// Determines the command name specified in the command-line arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to search for a command name. The collection is replaced with a copy with the command removed from it.</param>
    /// <returns>The name of the command that was found or <c>null</c> if none was specified.</returns>
    public static string? GetCommandName(ref IReadOnlyList<string> args)
    {
        #region Sanity checks
        if (args == null) throw new ArgumentNullException(nameof(args));
        #endregion

        var argsList = args.ToList();
        int index = argsList.FindIndex(arg => !arg.StartsWith("-") && !arg.StartsWith("/"));
        if (index == -1) return null;

        string commandName = argsList[index];
        argsList.RemoveAt(index);
        args = argsList;
        return commandName;
    }
}
