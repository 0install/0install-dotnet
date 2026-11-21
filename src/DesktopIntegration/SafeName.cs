// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Helpers for validating safe names for files and aliases.
/// </summary>
public static class SafeName
{
    private static readonly HashSet<string> _reserved = new(StringComparer.OrdinalIgnoreCase) {"0install", "0install-gui", "0install-win", "0launch", "0alias", "0store"};

    private static bool IsReserved(string value)
        => _reserved.Contains(value) || Paths.IsReservedDeviceName(value);

    private static bool IsPortable(string value)
        => Paths.IsPortableFileName(value)
           // Additional checks for shell safety
        && value[0] is not ('.' or '-')
        && !char.IsWhiteSpace(value[0]) && !char.IsWhiteSpace(value[^1])
        && !value.Any(IsInvalidChar);

    /// <summary>
    /// Indicates whether <paramref name="c"/> is not allowed in safe names.
    /// </summary>
    internal static bool IsInvalidChar(char c)
        => Paths.IsNonPortableFileNameChar(c)
        // Additional checks for shell safety
        || c is '=' or ';' or '\'';

    /// <summary>
    /// Indicates whether <paramref name="value"/> is a valid safe name.
    /// </summary>
    public static bool IsValid([NotNullWhen(true)] string? value)
        => !string.IsNullOrEmpty(value) && IsPortable(value) && !IsReserved(value);

    /// <summary>
    /// Ensures that <paramref name="value"/> is a valid safe name.
    /// </summary>
    /// <exception cref="IOException"><paramref name="value"/> is not a valid alias name.</exception>
    public static void Validate([System.Diagnostics.CodeAnalysis.NotNull] string? value)
    {
        if (string.IsNullOrEmpty(value)) throw new IOException(string.Format(Resources.NameInvalidChars, value));
        if (IsReserved(value)) throw new IOException(string.Format(Resources.NameReserved, value));
        if (!IsPortable(value)) throw new IOException(string.Format(Resources.NameInvalidChars, value));
    }
}
