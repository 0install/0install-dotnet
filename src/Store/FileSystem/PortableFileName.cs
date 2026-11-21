// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Rules for file names that are valid on all supported platforms.
/// </summary>
/// <remarks>Unlike <see cref="Path.GetInvalidFileNameChars"/> these rules do not depend on the current platform.</remarks>
public static class PortableFileName
{
    /// <summary>
    /// Indicates whether <paramref name="c"/> cannot be used in file names on at least one supported platform.
    /// </summary>
    public static bool IsInvalidChar(char c)
        => c is '/' or '\\' or ':' or '"' or '*' or '?' or '<' or '>' or '|' or '=' or ';' or '\'' || char.IsControl(c);

    /// <summary>
    /// Indicates whether <paramref name="value"/> can be used as a file name on all supported platforms.
    /// </summary>
    public static bool IsValid([NotNullWhen(true)] string? value)
        => !string.IsNullOrEmpty(value)
        && value[0] is not ('.' or '-')
        && value[^1] is not '.'
        && !char.IsWhiteSpace(value[0]) && !char.IsWhiteSpace(value[^1])
        && !value.Any(IsInvalidChar)
        && !WindowsDeviceNames.IsReserved(value);
}
