// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// File names that Windows reserves for devices.
/// </summary>
public static class WindowsDeviceNames
{
    private static readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$", "CLOCK$",
        "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM¹", "COM²", "COM³",
        "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT¹", "LPT²", "LPT³"
    };

    /// <summary>
    /// Indicates whether <paramref name="fileName"/> refers to a device on Windows.
    /// </summary>
    /// <param name="fileName">A file name without any directory part.</param>
    /// <remarks>Device names are reserved even with an extension (e.g., <c>NUL.txt</c>) and trailing spaces.</remarks>
    public static bool IsReserved(string fileName)
        => _names.Contains(fileName.Split('.')[0].TrimEnd(' '));
}
