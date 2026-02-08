// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using NanoByte.Common.Native;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Provides filesystem helper methods for working with implementation directories.
/// </summary>
public static class ImplFileUtils
{
    /// <summary>
    /// The name of the NTFS alternate stream used to mark a file as Unix-executable.
    /// </summary>
    private const string ExecutableIndicator = "xbit";

    /// <summary>
    /// The name of the NTFS extended attribute used to store Unix file mode (used by WSL).
    /// </summary>
    private const string LxModIndicator = "$LXMOD";

    /// <summary>
    /// Default Unix permission mode for executable files (0755 octal = rwxr-xr-x).
    /// </summary>
    private const int DefaultExecutableMode = 493; // 0755 in octal

    /// <summary>
    /// Checks whether a file is marked as Unix-executable.
    /// </summary>
    /// <param name="path">The path of the file to check.</param>
    /// <param name="manifestElement">The file's equivalent manifest entry, if available.</param>
    /// <returns><c>true</c> if <paramref name="path"/> points to an executable; <c>false</c> otherwise.</returns>
    public static bool IsExecutable(string path, ManifestElement? manifestElement = null)
    {
        if (manifestElement != null)
            return manifestElement is ManifestExecutableFile;
        if (FileUtils.IsExecutable(path))
            return true;
        if (WindowsUtils.IsWindowsNT)
        {
            // Check xbit attribute
            if (FileUtils.ReadExtendedMetadata(path, ExecutableIndicator) != null)
                return true;

            // Check $LXMOD attribute (used by WSL)
            if (IsExecutableFromLxMod(path))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a file has the executable bit set in its $LXMOD extended attribute.
    /// </summary>
    /// <param name="path">The path of the file to check.</param>
    /// <returns><c>true</c> if the file has executable permission in $LXMOD; <c>false</c> otherwise.</returns>
    private static bool IsExecutableFromLxMod(string path)
    {
        var lxmodData = FileUtils.ReadExtendedMetadata(path, LxModIndicator);
        if (lxmodData == null || lxmodData.Length < 4)
            return false;

        try
        {
            // $LXMOD stores Unix mode as a 32-bit little-endian integer
            int mode = BitConverter.ToInt32(lxmodData, 0);
            // Check if owner execute bit (0100 in octal) is set
            return (mode & 0x40) != 0; // 0x40 = 0100 octal = owner execute bit
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Retrieves the Unix mode from a file's $LXMOD extended attribute.
    /// </summary>
    /// <param name="path">The path of the file to check.</param>
    /// <returns>The Unix mode if present and valid; <c>null</c> otherwise.</returns>
    private static int? GetModeFromLxMod(string path)
    {
        var lxmodData = FileUtils.ReadExtendedMetadata(path, LxModIndicator);
        if (lxmodData == null || lxmodData.Length < 4)
            return null;

        try
        {
            return BitConverter.ToInt32(lxmodData, 0);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Marks a file as Unix-executable.
    /// </summary>
    /// <param name="fullPath">The absolute path of the file.</param>
    public static void SetExecutable(string fullPath)
    {
        var modifiedTime = File.GetLastWriteTimeUtc(fullPath);

        if (UnixUtils.IsUnix)
            FileUtils.SetExecutable(fullPath, true);
        else if (WindowsUtils.IsWindowsNT)
        {
            // Set xbit for backwards compatibility
            FileUtils.WriteExtendedMetadata(fullPath, ExecutableIndicator, []);

            // Set $LXMOD for WSL compatibility
            // Preserve existing permissions if present, otherwise use default
            int mode = GetModeFromLxMod(fullPath) ?? DefaultExecutableMode;
            // Set the owner execute bit (0x40 = 0100 octal)
            mode |= 0x40;
            byte[] modeBytes = BitConverter.GetBytes(mode);
            FileUtils.WriteExtendedMetadata(fullPath, LxModIndicator, modeBytes);
        }

        File.SetLastWriteTimeUtc(fullPath, modifiedTime);
    }

    /// <summary>
    /// Checks whether a file is a symbolic link.
    /// </summary>
    /// <param name="path">The path of the file to check.</param>
    /// <param name="manifestElement">The file's equivalent manifest entry, if available.</param>
    /// <param name="target">Returns the target the symbolic link points to if it exists.</param>
    /// <returns><c>true</c> if <paramref name="manifestElement"/> points to a symbolic link; <c>false</c> otherwise.</returns>
    public static bool IsSymlink(string path, [NotNullWhen(true)] out string? target, ManifestElement? manifestElement = null)
    {
        if (FileUtils.IsSymlink(path, out target) || WindowsUtils.IsWindowsNT && CygwinUtils.IsSymlink(path, out target))
            return true;

        if (manifestElement is ManifestSymlink)
        {
            target = File.ReadAllText(path, Encoding.UTF8);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a new symbolic link to a file or directory.
    /// </summary>
    /// <param name="sourcePath">The path of the link to create.</param>
    /// <param name="targetPath">The path of the existing file or directory to point to (relative to <paramref name="sourcePath" />).</param>
    public static void CreateSymlink(string sourcePath, string targetPath)
    {
        try
        {
            FileUtils.CreateSymlink(sourcePath, targetPath);
        }
        catch (IOException) when (WindowsUtils.IsWindows)
        {
            Log.Debug($"Creating Cygwin symlink instead of NTFS symlink due to insufficient permissions: {sourcePath}");
            CygwinUtils.CreateSymlink(sourcePath, targetPath);
        }
    }
}
