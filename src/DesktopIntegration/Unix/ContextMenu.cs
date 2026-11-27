// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text.RegularExpressions;

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Contains control logic for applying <see cref="Model.Capabilities.ContextMenu"/> and <see cref="AccessPoints.ContextMenu"/> for KDE and Nemo.
/// </summary>
public static class ContextMenu
{
    #region Constants
    private static readonly string[] _executableMimeTypes = ["application/x-executable", "application/x-pie-executable", "application/x-shellscript", "application/vnd.appimage", "application/x-ms-dos-executable", "application/vnd.microsoft.portable-executable"];

    private const string DirectoryMimeType = "inode/directory";
    #endregion

    #region Apply
    /// <summary>
    /// Adds a context menu entry to the current system.
    /// </summary>
    /// <param name="target">The application being integrated.</param>
    /// <param name="contextMenu">The context menu entry to add.</param>
    /// <param name="machineWide">Add the context menu entry machine-wide instead of just for the current user.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Apply(FeedTarget target, Model.Capabilities.ContextMenu contextMenu, IIconStore iconStore, bool machineWide)
    {
        #region Sanity checks
        if (contextMenu == null) throw new ArgumentNullException(nameof(contextMenu));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        if (contextMenu.Verbs is []) return;

        // Get icons before making any changes, because downloads may fail
        var entries = contextMenu.Verbs.Select(verb => (verb, icon: FreeDesktop.GetIconPath(target, verb.Command, iconStore))).ToList();

        var mimeTypes = GetMimeTypes(contextMenu, machineWide);
        foreach (bool terminal in new[] {false, true}) // KDE only supports choosing this for an entire file
        {
            string path = GetKdePath(contextMenu, terminal, machineWide);
            var matchingEntries = entries.Where(x => FreeDesktop.NeedsTerminal(target, x.verb.Command) == terminal).ToList();
            if (matchingEntries is []) KeyFile.Delete(path);
            else
            {
                GetKdeServiceMenu(target, contextMenu, matchingEntries, mimeTypes, CommandLine.GetLauncher(gui: !terminal), terminal)
                   .Save(path, executable: true); // Plasma 6 requires service menus to be executable
            }
        }

        foreach (var (verb, icon) in entries)
        {
            bool terminal = FreeDesktop.NeedsTerminal(target, verb.Command);
            GetNemoAction(target, contextMenu, verb, icon, CommandLine.GetLauncher(gui: !terminal), terminal)
               .Save(GetNemoPath(contextMenu, verb, machineWide));
        }
    }

    private static IReadOnlyCollection<string> GetMimeTypes(Model.Capabilities.ContextMenu contextMenu, bool machineWide)
        => contextMenu.Target switch
        {
            ContextMenuTarget.ExecutableFiles => _executableMimeTypes,
            ContextMenuTarget.Directories => [DirectoryMimeType],
            ContextMenuTarget.All => [MimeTypes.OctetStream, DirectoryMimeType],
            _ when contextMenu.Extensions is not [] => MimeTypes.Register(GetID(contextMenu), contextMenu.Extensions, description: null, machineWide).Select(x => x.Effective).Distinct().ToList(),
            _ => [MimeTypes.OctetStream]
        };

    /// <summary>
    /// Generates a KDE service menu. Multiple service menus for the same context menu are merged into one submenu.
    /// </summary>
    /// <seealso href="https://develop.kde.org/docs/apps/dolphin/service-menus/"/>
    internal static KeyFile GetKdeServiceMenu(FeedTarget target, Model.Capabilities.ContextMenu contextMenu, IReadOnlyList<(Verb verb, string? icon)> entries, IEnumerable<string> mimeTypes, string launcher, bool terminal)
    {
        var file = new KeyFile()
                  .Group("Desktop Entry")
                  .Add("Type", "Service")
                  .Add("X-KDE-ServiceTypes", "KonqPopupMenu/Plugin")
                  .Add("MimeType", mimeTypes)
                  .Add("Actions", entries.Select((_, i) => $"verb{i}"))
                  .Add("Terminal", terminal); // Ignored if set for individual actions
        if (contextMenu.Verbs.Count > 1)
        {
            file.Add("X-KDE-Submenu", GetLabel(contextMenu.Descriptions) ?? target.Feed.Name)
                .Add("Icon", entries[0].icon);
        }
        if (entries.All(x => x.verb.SingleElementOnly))
            file.Add("X-KDE-RequiredNumberOfUrls", "1");

        for (int i = 0; i < entries.Count; i++)
        {
            var (verb, icon) = entries[i];
            file.Group($"Desktop Action verb{i}")
                .Add("Name", GetLabel(target, contextMenu, verb))
                .Add("Icon", icon)
                .Add("Exec", CommandLine.GetRunArguments(target.Uri, verb).Prepend(launcher).ToExec(MimeHandler.FieldCodeFile));
        }

        return file;
    }

    /// <summary>
    /// Generates a Nemo action.
    /// </summary>
    internal static KeyFile GetNemoAction(FeedTarget target, Model.Capabilities.ContextMenu contextMenu, Verb verb, string? icon, string launcher, bool terminal)
    {
        var arguments = CommandLine.GetRunArguments(target.Uri, verb);
        bool supportsMultiple = !verb.SingleElementOnly && arguments.Contains(CommandLine.Item) && arguments.CountItems() == 1;

        var file = new KeyFile()
                  .Group("Nemo Action")
                  .Add("Active", true)
                  .Add("Name", EscapePercent(GetLabel(target, contextMenu, verb)))
                  .Add("Comment", EscapePercent(target.Feed.GetBestSummary(CultureInfo.CurrentUICulture, verb.Command.EmptyAsNull())))
                  .Add("Icon-Name", icon)
                  .Add("Exec", arguments.Prepend(launcher).ToExec(fieldCode: "%F"))
                  .Add("Quote", "double") // Required by older versions of Nemo to handle paths with spaces
                  .Add("Terminal", terminal)
                  .Add("Selection", supportsMultiple ? "notnone" : "s");
        return contextMenu.Target switch
        {
            ContextMenuTarget.ExecutableFiles => file.Add("Mimetypes", _executableMimeTypes),
            ContextMenuTarget.Directories => file.Add("Extensions", ["dir"]),
            ContextMenuTarget.All => file.Add("Extensions", ["any"]),
            _ when contextMenu.Extensions is not [] => file.Add("Extensions", contextMenu.Extensions.Select(x => x.Value.TrimStart('.')).Where(x => x.Length != 0)),
            _ => file.Add("Extensions", ["nodirs"])
        };

        // Nemo expands field codes in all text, not just in the command-line
        static string? EscapePercent(string? value) => value?.Replace("%", "%%");
    }

    private static string GetLabel(FeedTarget target, Model.Capabilities.ContextMenu contextMenu, Verb verb)
        => GetLabel(verb.Descriptions)
        ?? (contextMenu.Verbs is [_] ? GetLabel(contextMenu.Descriptions) ?? target.Feed.Name : verb.Name);

    /// <summary>
    /// Gets a label for a menu entry, with keyboard accelerators (e.g. "&amp;Open") removed.
    /// </summary>
    private static string? GetLabel(LocalizableStringCollection descriptions)
        => descriptions.GetBestLanguage(CultureInfo.CurrentUICulture)
                       .EmptyAsNull()
                      ?.To(x => Regex.Replace(x, "&(.)", "$1"));
    #endregion

    #region Remove
    /// <summary>
    /// Removes a context menu entry from the current system.
    /// </summary>
    /// <param name="contextMenu">The context menu entry to remove.</param>
    /// <param name="machineWide">Remove the context menu entry machine-wide instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Remove(Model.Capabilities.ContextMenu contextMenu, bool machineWide)
    {
        #region Sanity checks
        if (contextMenu == null) throw new ArgumentNullException(nameof(contextMenu));
        #endregion

        KeyFile.Delete(GetKdePath(contextMenu, terminal: false, machineWide));
        KeyFile.Delete(GetKdePath(contextMenu, terminal: true, machineWide));
        foreach (var verb in contextMenu.Verbs)
            KeyFile.Delete(GetNemoPath(contextMenu, verb, machineWide));
        MimeTypes.Unregister(GetID(contextMenu), machineWide);
    }
    #endregion

    /// <summary>
    /// Returns an ID for the context menu entry. Follows the same rules as <see cref="AccessPoints.ContextMenu.GetConflictIDs"/>.
    /// </summary>
    private static string GetID(Model.Capabilities.ContextMenu contextMenu)
        => $"context-menu/{contextMenu.Target}/{(contextMenu.Verbs is [var verb] ? verb.Name : contextMenu.ID)}";

    private static string GetKdePath(Model.Capabilities.ContextMenu contextMenu, bool terminal, bool machineWide)
        => Paths.Combine(FreeDesktop.GetDataDir(machineWide), "kio", "servicemenus", $"{FreeDesktop.FilePrefix}{FeedUri.Escape(GetID(contextMenu) + (terminal ? "/terminal" : ""))}.desktop");

    private static string GetNemoPath(Model.Capabilities.ContextMenu contextMenu, Verb verb, bool machineWide)
        => Paths.Combine(FreeDesktop.GetDataDir(machineWide), "nemo", "actions", $"{FreeDesktop.FilePrefix}{FeedUri.Escape($"{GetID(contextMenu)}/{verb.Name}")}.nemo_action");
}
