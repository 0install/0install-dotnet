// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Microsoft.Win32;
using NanoByte.Common.Native;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.Publish.Capture;

partial class SnapshotDiff
{
    /// <summary>
    /// Collects data about file types and also URL protocol handlers.
    /// </summary>
    /// <param name="commandMapper">Provides best-match command-line to <see cref="Command"/> mapping.</param>
    /// <param name="capabilities">The capability list to add the collected data to.</param>
    /// <exception cref="IOException">There was an error accessing the registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
    public void CollectFileTypes(CommandMapper commandMapper, CapabilityList capabilities)
    {
        #region Sanity checks
        if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));
        if (commandMapper == null) throw new ArgumentNullException(nameof(commandMapper));
        #endregion

        capabilities.Entries.Add((
            from progID in ProgIDs
            where !string.IsNullOrEmpty(progID)
            select GetFileType(progID, commandMapper)).WhereNotNull());
    }

    /// <summary>
    /// Retrieves data about a specific file type or URL protocol from a snapshot diff.
    /// </summary>
    /// <param name="progID">The programmatic identifier of the file type.</param>
    /// <param name="commandMapper">Provides best-match command-line to <see cref="Command"/> mapping.</param>
    /// <returns>Data about the file type or <see paramref="null"/> if no file type for this <paramref name="progID"/> was detected.</returns>
    /// <exception cref="IOException">There was an error accessing the registry.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the registry was not permitted.</exception>
    private VerbCapability? GetFileType(string progID, CommandMapper commandMapper)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(progID)) throw new ArgumentNullException(nameof(progID));
        if (commandMapper == null) throw new ArgumentNullException(nameof(commandMapper));
        #endregion

        using var progIDKey = Registry.ClassesRoot.TryOpenSubKey(progID);
        if (progIDKey == null) return null;

        VerbCapability capability;
        if (progIDKey.GetValue(DesktopIntegration.Windows.UrlProtocol.ProtocolIndicator) == null)
        { // Normal file type
            var fileType = new FileType {ID = progID};

            foreach ((string extension, string id) in FileAssocs)
            {
                if (id != progID || string.IsNullOrEmpty(extension)) continue;

                using var assocKey = Registry.ClassesRoot.TryOpenSubKey(extension);
                if (assocKey == null) continue;

                fileType.Extensions.Add(new()
                {
                    Value = extension,
                    MimeType = assocKey.GetValue(DesktopIntegration.Windows.FileType.RegValueContentType)?.ToString(),
                    PerceivedType = assocKey.GetValue(DesktopIntegration.Windows.FileType.RegValuePerceivedType)?.ToString()
                });
            }

            capability = fileType;
        }
        else
        { // URL protocol handler
            capability = new UrlProtocol {ID = progID};
        }

        string? description = (progIDKey.GetValue(DesktopIntegration.Windows.FileType.RegValueFriendlyName) ?? progIDKey.GetValue(""))?.ToString();
        if (!string.IsNullOrEmpty(description))
            capability.Descriptions.Add(description);

        capability.Verbs.Add(GetVerbs(progIDKey, commandMapper));

        // Only return capabilities that have verbs associated with them
        return capability.Verbs.Count == 0 ? null : capability;
    }
}
