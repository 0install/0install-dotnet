// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Maps file extensions to MIME types, adding missing mappings to the freedesktop.org Shared MIME-info Database.
/// </summary>
/// <seealso href="https://specifications.freedesktop.org/shared-mime-info-spec/latest/"/>
internal static partial class MimeTypes
{
    /// <summary>
    /// Prefix for MIME types generated for file extensions without a known MIME type.
    /// </summary>
    public const string GeneratedPrefix = "application/x-zeroinstall-";

    /// <summary>
    /// The MIME type for any kind of file.
    /// </summary>
    public const string OctetStream = "application/octet-stream";

    /// <summary>
    /// The MIME type for any kind of text file.
    /// </summary>
    private const string PlainText = "text/plain";

    private static readonly XNamespace _namespace = "http://www.freedesktop.org/standards/shared-mime-info";

    private const string MimeTypePattern = @"^[A-Za-z0-9][A-Za-z0-9!#$&^_.+-]*/[A-Za-z0-9][A-Za-z0-9!#$&^_.+-]*$";
#if NET
    [GeneratedRegex(MimeTypePattern)]
    private static partial Regex MimeTypeRegex();
#else
    private static readonly Regex _mimeTypeRegex = new(MimeTypePattern);
    private static Regex MimeTypeRegex() => _mimeTypeRegex;
#endif

    /// <summary>
    /// Returns the directory containing the Shared MIME-info Database.
    /// </summary>
    public static string GetDir(bool machineWide)
        => Paths.Combine(FreeDesktop.GetDataDir(machineWide), "mime");

    /// <summary>
    /// The MIME type to use for a file extension.
    /// </summary>
    /// <param name="Extension">The file extension.</param>
    /// <param name="Effective">The MIME type files with this extension are detected as.</param>
    /// <param name="Declared">The MIME type specified in the feed; <c>null</c> if there is none or it is too generic to be useful.</param>
    /// <param name="IsKnown">Indicates that the system already maps the extension to <paramref name="Effective"/>.</param>
    internal record Mapping(FileTypeExtension Extension, string Effective, string? Declared, bool IsKnown);

    /// <summary>
    /// Determines the MIME types for file extensions, adding missing ones to the database.
    /// </summary>
    /// <param name="id">A unique ID for this set of file extensions. Used for <see cref="Unregister"/>.</param>
    /// <param name="extensions">The file extensions.</param>
    /// <param name="description">A human-readable description of the file type; can be <c>null</c>.</param>
    /// <param name="machineWide">Add to the machine-wide database instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static IReadOnlyList<Mapping> Register(string id, IEnumerable<FileTypeExtension> extensions, string? description, bool machineWide)
    {
        string targetDir = GetDir(machineWide);
        string[] systemDirs = Locations.SystemDataDirs.Split([Path.PathSeparator], StringSplitOptions.RemoveEmptyEntries)
                                       .Select(x => Paths.Combine(x, "mime"))
                                       .Where(x => !Paths.AreEqual(x, targetDir))
                                       .ToArray();

        string path = GetPackagePath(id, machineWide);
        var mappings = extensions.Where(x => !string.IsNullOrEmpty(x.Value))
                                 .Select(x => GetMapping(x, targetDir, ownPackagePath: path, systemDirs))
                                 .ToList();

        if (mappings.All(x => x.IsKnown)) Unregister(id, machineWide);
        else
        {
            using var atomic = new AtomicWrite(path);
            GetPackage(mappings, description, isKnownType: type => systemDirs.Any(x => File.Exists(Paths.Combine(x, $"{type}.xml"))))
               .Save(atomic.WritePath);
            atomic.Commit();
        }

        return mappings;
    }

    /// <summary>
    /// Removes MIME types that were added to the database.
    /// </summary>
    /// <param name="id">The unique ID passed to <see cref="Register"/>.</param>
    /// <param name="machineWide">The MIME types were added to the machine-wide database instead of just for the current user.</param>
    /// <exception cref="IOException">A problem occurred while writing to the filesystem.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the filesystem is not permitted.</exception>
    public static void Unregister(string id, bool machineWide)
    {
        string path = GetPackagePath(id, machineWide);
        if (File.Exists(path)) File.Delete(path);
    }

    private static string GetPackagePath(string id, bool machineWide)
        => Paths.Combine(GetDir(machineWide), "packages", $"{FreeDesktop.FilePrefix}{FeedUri.Escape(id)}.xml");

    private static Mapping GetMapping(FileTypeExtension extension, string targetDir, string ownPackagePath, IEnumerable<string> systemDirs)
    {
        string? declared = extension.MimeType.EmptyAsNull();
        if (declared is OctetStream or PlainText || (declared != null && !MimeTypeRegex().IsMatch(declared)))
            declared = null; // Too generic, would affect unrelated files

        string pattern = GetPattern(extension);
        string? known = GetBestMatch(GetPackageGlobs(targetDir, x => !Paths.FileName(x).StartsWithOrdinal(FreeDesktop.FilePrefix)), pattern)
                     ?? systemDirs.Select(x => GetBestMatch(GetGlobs(x), pattern)).FirstOrDefault(x => x != null);

        // Reuse MIME types we already added for other capabilities, so that the same extension is not mapped to multiple MIME types
        string? addedByUs = GetBestMatch(GetPackageGlobs(targetDir, x => Paths.FileName(x).StartsWithOrdinal(FreeDesktop.FilePrefix) && !Paths.AreEqual(x, ownPackagePath)), pattern);

        return new(extension, known ?? addedByUs ?? declared ?? Generate(extension), declared, IsKnown: known != null);
    }

    private static string GetPattern(FileTypeExtension extension)
        => "*." + extension.Value.TrimStart('.').ToLowerInvariant();

    private static string Generate(FileTypeExtension extension)
        => GeneratedPrefix + new string(GetPattern(extension)[2..].Select(x => x is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '+' ? x : '-').ToArray());

    /// <summary>
    /// Maps a filename pattern to a MIME type.
    /// </summary>
    private record Glob(string Type, string Pattern, int Weight, bool CaseSensitive);

    /// <summary>
    /// Returns the MIME type with the highest weight that a filename pattern is mapped to; <c>null</c> if there is none.
    /// </summary>
    private static string? GetBestMatch(IEnumerable<Glob> globs, string pattern)
        => globs.Where(x => x.CaseSensitive ? x.Pattern == pattern : StringUtils.EqualsIgnoreCase(x.Pattern, pattern))
                .Where(x => MimeTypeRegex().IsMatch(x.Type))
                .OrderByDescending(x => x.Weight)
                .FirstOrDefault()?.Type;

    /// <summary>
    /// Returns all filename patterns in a MIME database.
    /// </summary>
    private static IEnumerable<Glob> GetGlobs(string mimeDir)
    {
        string path = Paths.Combine(mimeDir, "globs2");
        if (!File.Exists(path)) return [];

        return from line in File.ReadLines(path)
               where !line.StartsWithOrdinal("#")
               let parts = line.Split(':') // weight:type:pattern[:flags]
               where parts.Length >= 3
               select new Glob(
                   Type: parts[1],
                   Pattern: parts[2],
                   Weight: ParseWeight(parts[0]) ?? 0,
                   CaseSensitive: parts.Length >= 4 && parts[3].Split(',').Contains("cs"));
    }

    /// <summary>
    /// Returns all filename patterns in selected source packages of a MIME database.
    /// </summary>
    /// <param name="mimeDir">The MIME database directory.</param>
    /// <param name="include">Selects the paths of the packages to read.</param>
    /// <remarks>Reads the source packages, because the generated <c>globs2</c> file may still contain patterns from packages we have already removed.</remarks>
    private static IEnumerable<Glob> GetPackageGlobs(string mimeDir, Func<string, bool> include)
    {
        string packagesDir = Paths.Combine(mimeDir, "packages");
        if (!Directory.Exists(packagesDir)) return [];

        return from path in Directory.GetFiles(packagesDir, "*.xml")
               where include(path)
               let package = TryLoad(path)
               where package != null
               from type in package.Root!.Elements(_namespace + "mime-type")
               from glob in type.Elements(_namespace + "glob")
               select new Glob(
                   Type: type.Attribute("type")?.Value ?? "",
                   Pattern: glob.Attribute("pattern")?.Value ?? "",
                   Weight: ParseWeight(glob.Attribute("weight")?.Value) ?? 50, // Default value according to specification
                   CaseSensitive: glob.Attribute("case-sensitive")?.Value == "true");
    }

    private static int? ParseWeight(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int weight) ? weight : null;

    private static XDocument? TryLoad(string path)
    {
        try
        {
            return XDocument.Load(path);
        }
        #region Error handling
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            Log.Warn(string.Format(Resources.ProblemLoading, path), ex);
            return null;
        }
        #endregion
    }

    private static XDocument GetPackage(IEnumerable<Mapping> mappings, string? description, Func<string, bool> isKnownType)
        => new(
            new XComment(" Generated by Zero Install "),
            new XElement(_namespace + "mime-info",
                from mapping in mappings
                where !mapping.IsKnown
                group mapping by mapping.Effective into type
                select new XElement(_namespace + "mime-type",
                    new XAttribute("type", type.Key),
                    isKnownType(type.Key) ? null : GetMetadata(type.Select(x => x.Extension).ToList(), description),
                    type.Select(x => GetPattern(x.Extension)).Distinct()
                        .Select(x => new XElement(_namespace + "glob", new XAttribute("pattern", x))))));

    private static IEnumerable<XElement> GetMetadata(IReadOnlyCollection<FileTypeExtension> extensions, string? description)
    {
        if (!string.IsNullOrEmpty(description))
            yield return new(_namespace + "comment", description);

        string? perceivedType = extensions.Select(x => x.PerceivedType).FirstOrDefault(x => !string.IsNullOrEmpty(x));

        if (perceivedType == FileTypeExtension.TypeText || extensions.Any(x => x.MimeType == PlainText))
            yield return new(_namespace + "sub-class-of", new XAttribute("type", PlainText));

        string? icon = perceivedType switch
        {
            FileTypeExtension.TypeText => "text-x-generic",
            FileTypeExtension.TypeImage => "image-x-generic",
            FileTypeExtension.TypeAudio => "audio-x-generic",
            FileTypeExtension.TypeVideo => "video-x-generic",
            FileTypeExtension.TypeCompressed => "package-x-generic",
            FileTypeExtension.TypeDocument => "x-office-document",
            FileTypeExtension.TypeApplication => "application-x-executable",
            _ => null
        };
        if (icon != null)
            yield return new(_namespace + "generic-icon", new XAttribute("name", icon));
    }
}
