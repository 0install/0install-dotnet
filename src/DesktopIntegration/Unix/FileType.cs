// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Contains control logic for applying <see cref="Model.Capabilities.FileType"/> and <see cref="AccessPoints.FileType"/> on freedesktop.org systems.
/// </summary>
public static class FileType
{
    #region Register
    /// <summary>
    /// Registers a file type in the current system.
    /// </summary>
    /// <param name="target">The application being integrated.</param>
    /// <param name="fileType">The file type to register.</param>
    /// <param name="machineWide">Register the file type machine-wide instead of just for the current user.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    /// <param name="accessPoint">Indicates that the file associations shall become default handlers for their respective types.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Register(FeedTarget target, Model.Capabilities.FileType fileType, IIconStore iconStore, bool machineWide, bool accessPoint = false)
    {
        #region Sanity checks
        if (fileType == null) throw new ArgumentNullException(nameof(fileType));
        if (iconStore == null) throw new ArgumentNullException(nameof(iconStore));
        #endregion

        if (fileType.GetOpenVerb() is not {} verb) return;

        // Get icon before making any changes, because downloads may fail
        FreeDesktop.GetIconPath(target, verb.Command, iconStore);

        var mappings = MimeTypes.Register(GetID(fileType), fileType.Extensions, fileType.Descriptions.GetBestLanguage(CultureInfo.CurrentUICulture), machineWide);
        var mimeTypes = accessPoint
            ? mappings.Select(x => x.Effective).Where(x => x != MimeTypes.OctetStream) // Do not become default handler for unrelated files
            : mappings.SelectMany(x => new[] {x.Effective, x.Declared}).WhereNotNull();
        MimeHandler.Register(target, verb, MimeHandler.FieldCodeFile, GetID(fileType), mimeTypes, iconStore, machineWide, accessPoint);
    }
    #endregion

    #region Unregister
    /// <summary>
    /// Unregisters a file type in the current system.
    /// </summary>
    /// <param name="fileType">The file type to remove.</param>
    /// <param name="machineWide">Unregister the file type machine-wide instead of just for the current user.</param>
    /// <param name="accessPoint">Indicates that the file associations were default handlers for their respective types.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Unregister(Model.Capabilities.FileType fileType, bool machineWide, bool accessPoint = false)
    {
        #region Sanity checks
        if (fileType == null) throw new ArgumentNullException(nameof(fileType));
        #endregion

        if (MimeHandler.Unregister(GetID(fileType), machineWide, accessPoint))
            MimeTypes.Unregister(GetID(fileType), machineWide);
    }
    #endregion

    private static string GetID(Model.Capabilities.FileType fileType) => $"file-type/{fileType.ID}";
}
