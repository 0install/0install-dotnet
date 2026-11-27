// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Registers applications as handlers for MIME types using hidden Desktop Entries.
/// </summary>
/// <remarks>
/// Capabilities that use the same command-line share a single Desktop Entry, so that applications are not listed multiple times as possible handlers.
/// The Desktop Entry keeps track of which capabilities it belongs to using <see cref="Claim"/>s.
/// </remarks>
internal static class MimeHandler
{
    private const string FilePrefix = FreeDesktop.FilePrefix + "handler-";
    private const string MimeTypeKey = "MimeType", ClaimsKey = "X-ZeroInstall-Claims";

    /// <summary>
    /// Field code for passing a single local file.
    /// </summary>
    public const string FieldCodeFile = "%f";

    /// <summary>
    /// Field code for passing a single URL.
    /// </summary>
    public const string FieldCodeUrl = "%u";

    /// <summary>
    /// Associates a capability with the MIME types it handles.
    /// </summary>
    /// <param name="CapabilityID">Uniquely identifies the capability across all applications.</param>
    /// <param name="IsDefault"><c>true</c> if the application is the default handler; <c>false</c> if it is just one of the possible handlers.</param>
    /// <param name="MimeTypes">The MIME types.</param>
    private record Claim(string CapabilityID, bool IsDefault, IReadOnlyCollection<string> MimeTypes)
    {
        public override string ToString()
            => $"{Uri.EscapeDataString(CapabilityID)}|{(IsDefault ? "default" : "candidate")}|{string.Join(",", MimeTypes)}";

        public static Claim? TryParse(string value)
            => value.Split('|') is [var capabilityID, var purpose, var mimeTypes]
                ? new(Uri.UnescapeDataString(capabilityID), purpose == "default", mimeTypes.Split(','))
                : null;
    }

    /// <summary>
    /// Returns the verb to use when opening files or URLs; <c>null</c> if there is none.
    /// </summary>
    public static Verb? GetOpenVerb(this VerbCapability capability)
        => capability.Verbs.FirstOrDefault(x => x.Name == Verb.NameOpen)
        ?? capability.Verbs.FirstOrDefault(x => !x.Extended)
        ?? capability.Verbs.FirstOrDefault();

    /// <summary>
    /// Registers an application as a handler for MIME types.
    /// </summary>
    /// <param name="target">The application being integrated.</param>
    /// <param name="verb">The verb used to open files or URLs.</param>
    /// <param name="fieldCode">The field code for passing the files or URLs to be opened.</param>
    /// <param name="capabilityID">Uniquely identifies the capability across all applications.</param>
    /// <param name="mimeTypes">The MIME types to handle.</param>
    /// <param name="iconStore">Stores icon files downloaded from the web as local files.</param>
    /// <param name="machineWide">Register machine-wide instead of just for the current user.</param>
    /// <param name="isDefault">Make the application the default handler, rather than just one of the possible handlers.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="WebException">A problem occurred while downloading additional data (such as icons).</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Register(FeedTarget target, Verb verb, string fieldCode, string capabilityID, IEnumerable<string> mimeTypes, IIconStore iconStore, bool machineWide, bool isDefault)
    {
        var claim = new Claim(capabilityID, isDefault, mimeTypes.Distinct().ToList());
        if (claim.MimeTypes.Count == 0) return;

        var arguments = CommandLine.GetRunArguments(target.Uri, verb);

        // Exclude launcher path from hash, so that the ID remains the same if the launcher moves
        string desktopID = $"{FilePrefix}{arguments.ToExec(fieldCode).HashSha256()[..16]}.desktop";
        string path = Paths.Combine(FreeDesktop.GetApplicationsDir(machineWide), desktopID);

        // Prepare everything that may fail due to external factors before making any changes
        var desktopEntry = FreeDesktop.GetDesktopEntry(target,
                                           target.Feed.GetBestName(CultureInfo.CurrentUICulture, verb.Command.EmptyAsNull()),
                                           verb.Command,
                                           arguments,
                                           fieldCode,
                                           iconStore)
                                      .Add("NoDisplay", true);

        // Remove any previous registrations that may have used a different command-line or different MIME types
        Unregister(capabilityID, machineWide, isDefault, keepDesktopID: desktopID);

        List<Claim> claims = [..ReadClaims(path), claim];
        desktopEntry.Add(MimeTypeKey, GetMimeTypes(claims))
                    .Add(ClaimsKey, claims.Select(x => x.ToString()))
                    .Save(path);

        if (isDefault)
        {
            MimeAppsList.Edit(machineWide, list =>
            {
                foreach (string mimeType in claim.MimeTypes)
                    list.AddDefault(mimeType, desktopID);
            });
        }
    }

    /// <summary>
    /// Unregisters an application as a handler for MIME types.
    /// </summary>
    /// <param name="capabilityID">Uniquely identifies the capability across all applications.</param>
    /// <param name="machineWide">The application was registered machine-wide instead of just for the current user.</param>
    /// <param name="isDefault">The application was made the default handler, rather than just one of the possible handlers.</param>
    /// <returns><c>true</c> if the capability is no longer registered at all; <c>false</c> if it is still registered with the opposite value for <paramref name="isDefault"/>.</returns>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static bool Unregister(string capabilityID, bool machineWide, bool isDefault)
        => Unregister(capabilityID, machineWide, isDefault, keepDesktopID: null);

    private static bool Unregister(string capabilityID, bool machineWide, bool isDefault, string? keepDesktopID)
    {
        string dirPath = FreeDesktop.GetApplicationsDir(machineWide);
        if (!Directory.Exists(dirPath)) return true;

        bool stillRegistered = false;
        foreach (string path in Directory.GetFiles(dirPath, $"{FilePrefix}*.desktop"))
        {
            var claims = ReadClaims(path);
            stillRegistered |= claims.Any(x => x.CapabilityID == capabilityID && x.IsDefault != isDefault);

            var remaining = claims.Where(x => x.CapabilityID != capabilityID || x.IsDefault != isDefault).ToList();
            if (remaining.Count == claims.Count) continue;

            string desktopID = Paths.FileName(path);
            if (remaining.Count == 0 && desktopID != keepDesktopID)
            {
                File.Delete(path);
                MimeAppsList.Edit(machineWide, list => list.RemoveAll(desktopID));
            }
            else
            {
                WriteClaims(path, remaining);

                var noLongerDefault = GetMimeTypes(claims.Where(x => x.IsDefault))
                                     .Except(GetMimeTypes(remaining.Where(x => x.IsDefault)))
                                     .ToList();
                MimeAppsList.Edit(machineWide, list =>
                {
                    foreach (string mimeType in noLongerDefault)
                        list.RemoveDefault(mimeType, desktopID);
                });
            }
        }
        return !stillRegistered;
    }

    private static IEnumerable<string> GetMimeTypes(IEnumerable<Claim> claims)
        => claims.SelectMany(x => x.MimeTypes).Distinct();

    private static IReadOnlyList<Claim> ReadClaims(string path)
    {
        if (!File.Exists(path) || !KeyFile.IsGenerated(path)) return [];

        return File.ReadLines(path)
                   .Where(x => x.StartsWithOrdinal(ClaimsKey + "="))
                   .SelectMany(x => x[(ClaimsKey.Length + 1)..].Split([';'], StringSplitOptions.RemoveEmptyEntries))
                   .Select(Claim.TryParse)
                   .WhereNotNull()
                   .ToList();
    }

    private static void WriteClaims(string path, IReadOnlyCollection<Claim> claims)
    {
        var lines = File.ReadAllLines(path).Select(line =>
            line.StartsWithOrdinal(MimeTypeKey + "=") ? $"{MimeTypeKey}={KeyFile.FormatList(GetMimeTypes(claims))}"
            : line.StartsWithOrdinal(ClaimsKey + "=") ? $"{ClaimsKey}={KeyFile.FormatList(claims.Select(x => x.ToString()))}"
            : line);

        using var atomic = new AtomicWrite(path);
        File.WriteAllText(atomic.WritePath, string.Concat(lines.Select(x => x + "\n")));
        atomic.Commit();
    }
}
