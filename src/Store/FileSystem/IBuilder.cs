// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Builds a file system directory.
/// </summary>
public interface IBuilder : IForwardOnlyBuilder
{
    /// <summary>
    /// Renames a file or directory in the implementation.
    /// </summary>
    /// <param name="path">The original path of the file or directory relative to the implementation root.</param>
    /// <param name="target">The new path of the file or directory relative to the implementation root.</param>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    void Rename(string path, string target);

    /// <summary>
    /// Removes a file or directory from the implementation.
    /// </summary>
    /// <param name="path">The path of the file or directory relative to the implementation root.</param>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    void Remove(string path);

    /// <summary>
    /// Marks a previously added file as executable.
    /// </summary>
    /// <param name="path">The path of the file to create relative to the implementation root.</param>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    void MarkAsExecutable(string path);

    /// <summary>
    /// Turns a previously added file into a symlink.
    /// </summary>
    /// <param name="path">The path of the symlink to create relative to the implementation root.</param>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    void TurnIntoSymlink(string path);
}
