// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.DesktopIntegration;

/// <summary>
/// Helpers for validating safe names for files and aliases.
/// </summary>
public static class SafeName
{
    private static readonly HashSet<string> _reserved = new(StringComparer.OrdinalIgnoreCase) {"0install", "0install-gui", "0install-win", "0launch", "0alias", "0store"};

    private static bool IsReserved(string value)
        => _reserved.Contains(value) || WindowsDeviceNames.IsReserved(value);

    /// <summary>
    /// Indicates whether <paramref name="value"/> is a valid alias name.
    /// </summary>
    public static bool IsValid([NotNullWhen(true)] string? value)
        => PortableFileName.IsValid(value) && !IsReserved(value);

    /// <summary>
    /// Ensures that <paramref name="value"/> is a valid alias name.
    /// </summary>
    /// <exception cref="IOException"><paramref name="value"/> is not a valid alias name.</exception>
    public static void Validate([System.Diagnostics.CodeAnalysis.NotNull] string? value)
    {
        if (string.IsNullOrEmpty(value)) throw new IOException(string.Format(Resources.NameInvalidChars, value));
        if (IsReserved(value)) throw new IOException(string.Format(Resources.NameReserved, value));
        if (!PortableFileName.IsValid(value)) throw new IOException(string.Format(Resources.NameInvalidChars, value));
    }
}
