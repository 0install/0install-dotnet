// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Helpers for validating and constructing pet-names — user-defined identifiers for <see cref="AppEntry"/> instances created with custom <see cref="Requirements"/>.
/// </summary>
public static class PetName
{
    private static readonly HashSet<string> _reserved = new(StringComparer.OrdinalIgnoreCase) {"0install", "0store"};

    /// <summary>
    /// Indicates whether <paramref name="value"/> is a syntactically valid pet-name.
    /// </summary>
    /// <remarks>
    /// Valid pet-names are non-empty, contain none of <c>/ \ : = ; ' "</c>, do not start with <c>.</c>,
    /// are not equal to <c>.</c> or <c>..</c>, and are not a reserved name.
    /// </remarks>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (value is "." or "..") return false;
        if (value[0] == '.') return false;
        foreach (char c in value)
        {
            if (c is '/' or '\\' or ':' or '=' or ';' or '\'' or '"') return false;
        }
        return !_reserved.Contains(value);
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="value"/> is not a valid pet-name.
    /// </summary>
    public static void Validate(string? value, string paramName = "petName")
    {
        if (!IsValid(value)) throw new ArgumentException(string.Format(Resources.InvalidPetName, value), paramName);
    }

    /// <summary>
    /// Wraps a pet-name in a <see cref="FeedUri"/> with the <see cref="FeedUri.PetNameScheme"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid pet-name.</exception>
    public static FeedUri ToUri(string value)
    {
        Validate(value);
        return new FeedUri($"{FeedUri.PetNameScheme}:{value}");
    }
}
