// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Services.Executors;

/// <summary>
/// Translates paths between the host system and target execution environment.
/// </summary>
public interface IPathMapper
{
    /// <summary>
    /// Translates a path from the host system to the target execution environment.
    /// </summary>
    /// <param name="hostPath">The path on the host system.</param>
    /// <returns>The translated path in the target environment.</returns>
    string MapPath(string hostPath);

    /// <summary>
    /// Translates a path from the target execution environment back to the host system.
    /// </summary>
    /// <param name="targetPath">The path in the target environment.</param>
    /// <returns>The translated path on the host system.</returns>
    string UnmapPath(string targetPath);
}
