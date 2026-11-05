// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Strict validation of paths and digests received by <see cref="StoreServiceServer"/>.
/// </summary>
internal static class StoreServicePaths
{
    private static readonly char[] _invalidChars = ['<', '>', ':', '"', '|', '?', '*', '\\'];

    private static readonly HashSet<string> _reservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$", "CLOCK$",
        "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM¹", "COM²", "COM³",
        "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT¹", "LPT²", "LPT³"
    };

    /// <summary>
    /// Validates a relative path received on the wire and converts it to a native path.
    /// </summary>
    /// <param name="path">A relative path using <c>/</c> as the separator.</param>
    /// <returns>The path using native separators.</returns>
    /// <exception cref="InvalidDataException"><paramref name="path"/> is not a safe relative path.</exception>
    public static string ToNativePath(string path)
    {
        if (path.Length is 0 or > StoreServiceProtocol.MaxPathLength) throw Invalid(path);

        string[] components = path.Split('/');
        if (components.Length > StoreServiceProtocol.MaxPathDepth) throw Invalid(path);
        foreach (string component in components)
        {
            if (!IsValidComponent(component)) throw Invalid(path);
        }

        return string.Join(Path.DirectorySeparatorChar.ToString(), components);
    }

    private static bool IsValidComponent(string component)
    {
        if (component.Length is 0 or > StoreServiceProtocol.MaxPathComponentLength) return false;
        if (component is "." or "..") return false;
        if (component.EndsWith(".") || component.EndsWith(" ")) return false; // Windows silently strips these, causing aliasing
        if (component.Any(c => c < 0x20 || c == 0x7F)) return false;
        if (component.IndexOfAny(_invalidChars) >= 0) return false;

        // Device names are reserved even with an extension (e.g., "NUL.txt")
        string baseName = component.Split('.')[0].TrimEnd(' ');
        return !_reservedNames.Contains(baseName);
    }

    /// <summary>
    /// Validates a symlink target received on the wire. Symlink targets are stored as data and never followed by the service.
    /// </summary>
    /// <exception cref="InvalidDataException"><paramref name="target"/> is empty or contains control characters.</exception>
    public static string ValidateSymlinkTarget(string target)
    {
        if (target.Length == 0 || target.Any(c => c < 0x20)) throw new InvalidDataException("Invalid symlink target.");
        return target;
    }

    private static InvalidDataException Invalid(string path)
        => new($"Invalid path: {Escape(path)}");

    /// <summary>
    /// Escapes characters that could be used to forge or hide parts of log entries and error messages (line breaks, other control characters, bidirectional overrides, etc.).
    /// </summary>
    /// <param name="value">A string received from a client.</param>
    /// <returns>The string with problematic characters (and backslashes, to keep the result unambiguous) replaced by <c>\uXXXX</c> escape sequences.</returns>
    public static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            if (c == '\\' || CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator)
                builder.Append($"\\u{(int)c:X4}");
            else builder.Append(c);
        }
        return builder.ToString();
    }

    private static readonly Regex
        _sha256New = new(@"\Asha256new_[A-Z2-7]{52}\z", RegexOptions.CultureInvariant),
        _sha256 = new(@"\Asha256=[0-9a-f]{64}\z", RegexOptions.CultureInvariant),
        _sha1 = new(@"\Asha1(new)?=[0-9a-f]{40}\z", RegexOptions.CultureInvariant);

    /// <summary>
    /// Validates digests received on the wire and selects the one to use.
    /// </summary>
    /// <param name="digests">Digest strings in the form <c>algorithm=value</c> or <c>algorithm_value</c>.</param>
    /// <returns>A <see cref="ManifestDigest"/> containing only the strongest digest.</returns>
    /// <exception cref="InvalidDataException">A digest is malformed.</exception>
    /// <exception cref="NotSupportedException">None of the digests uses a SHA-256 based algorithm.</exception>
    public static ManifestDigest ToStrongDigest(IEnumerable<string> digests)
    {
        string? sha256New = null, sha256 = null;
        foreach (string digest in digests)
        {
            if (_sha256New.IsMatch(digest)) sha256New ??= digest;
            else if (_sha256.IsMatch(digest)) sha256 ??= digest;
            else if (!_sha1.IsMatch(digest)) throw new InvalidDataException($"Invalid digest: {Escape(digest)}");
        }

        if (sha256New != null) return new(sha256New);
        if (sha256 != null) return new(sha256);
        throw new NotSupportedException("The Store Service only accepts implementations identified by SHA-256 based digests.");
    }
}
