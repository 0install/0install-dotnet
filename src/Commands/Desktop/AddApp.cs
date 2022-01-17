// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Net;
using NanoByte.Common.Native;
using NanoByte.Common.Tasks;
using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Model;
using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Desktop;

/// <summary>
/// Add an application to the <see cref="AppList"/>.
/// </summary>
public class AddApp : AppCommand
{
    public const string Name = "add";
    public const string AltName = "add-app";
    public override string Description => Resources.DescriptionAddApp;
    public override string Usage => "[OPTIONS] [ALIAS] INTERFACE";
    protected override int AdditionalArgsMax => 2;

    private string? _command;

    /// <inheritdoc/>
    public AddApp(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("no-download", () => Resources.OptionNoDownload, _ => NoDownload = true);
        Options.Add("command=", () => Resources.OptionCommand, command => _command = command);
    }

    /// <summary>
    /// The window message ID (for use with <see cref="WindowsUtils.BroadcastMessage"/>) that signals that an application that is not listed in the <see cref="Catalog"/> was added.
    /// </summary>
    public static readonly int AddedNonCatalogAppWindowMessageID;

    static AddApp()
    {
        if (WindowsUtils.IsWindows)
            AddedNonCatalogAppWindowMessageID = WindowsUtils.RegisterWindowMessage("ZeroInstall.Commands.AddedNonCatalogApp");
    }

    /// <inheritdoc/>
    protected override ExitCode ExecuteHelper()
    {
        try
        {
            var appEntry = GetAppEntry(IntegrationManager, ref InterfaceUri);

            if (AdditionalArgs.Count == 2)
                CreateAlias(appEntry, AdditionalArgs[0], _command);
            else if (_command != null)
                throw new OptionException(Resources.NoAddCommandWithoutAlias, "command");

            if (WindowsUtils.IsWindows && !CatalogManager.GetCachedSafe().ContainsFeed(appEntry.InterfaceUri))
                WindowsUtils.BroadcastMessage(AddedNonCatalogAppWindowMessageID); // Notify Zero Install GUIs of changes

            return ExitCode.OK;
        }
        #region Error handling
        catch (InvalidOperationException ex)
            // WebException is a subclass of InvalidOperationException but we don't want to catch it here
            when (ex is not WebException)
        { // Application already in AppList
            Handler.OutputLow(Resources.DesktopIntegration, ex.Message);
            return ExitCode.NoChanges;
        }
        #endregion
    }
}
