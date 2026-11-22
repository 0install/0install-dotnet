// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;

namespace ZeroInstall.DesktopIntegration.Unix;

/// <summary>
/// Manages the PATH environment variable.
/// </summary>
internal static class PathEnv
{
    /// <summary>
    /// Adds a directory to the search PATH by editing shell profile files, unless it is already in the PATH.
    /// </summary>
    /// <param name="dirPath">The absolute path of the directory to add.</param>
    public static void AddDir(string dirPath)
    {
        if (Locations.IsInSearchPath(dirPath)) return;

        string profileDirPath = ToProfileSyntax(dirPath);
        foreach (string profilePath in GetProfilePaths())
        {
            if (File.Exists(profilePath) && File.ReadAllText(profilePath) is var content && (content.Contains(dirPath) || content.Contains(profileDirPath)))
                continue;

            File.AppendAllText(profilePath, $"\nexport PATH=\"{profileDirPath}:$PATH\"\n");
            Log.Warn(string.Format(Resources.PathAddedToProfile, dirPath, profilePath));
        }
    }

    /// <summary>
    /// Expresses paths in the user's home directory relative to <c>$HOME</c>.
    /// </summary>
    private static string ToProfileSyntax(string dirPath)
    {
        string homeDir = Locations.HomeDir.TrimEnd('/') + "/";
        return dirPath.StartsWithOrdinal(homeDir)
            ? "$HOME/" + dirPath[homeDir.Length..]
            : dirPath;
    }

    /// <summary>
    /// Returns the paths of the shell profile files that login shells of the current user are likely to read.
    /// </summary>
    private static IEnumerable<string> GetProfilePaths()
    {
        bool shellIsZsh = (Environment.GetEnvironmentVariable("SHELL") ?? "").EndsWithOrdinal("/zsh");

        // Bash only reads ~/.profile if neither of these exist
        string? bashProfile = new[] {".bash_profile", ".bash_login"}.Select(Home).FirstOrDefault(File.Exists);

        string profile = Home(".profile");
        if (File.Exists(profile) || bashProfile == null && !shellIsZsh)
            yield return profile;

        if (bashProfile != null) yield return bashProfile;

        string zprofile = Home(".zprofile");
        if (File.Exists(zprofile) || UnixUtils.IsMacOSX || shellIsZsh)
            yield return zprofile;

        string Home(string fileName) => Paths.Combine(Locations.HomeDir, fileName);
    }
}
