// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Helpers for adding <see cref="RetrievalMethod"/>s to <see cref="IBuilder"/>s.
/// </summary>
public static class BuilderExtensions
{
    /// <summary>
    /// Adds a subdirectory to the implementation and returns a wrapped <see cref="IBuilder"/> to elements inside this subdirectory.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="path">The path of the directory to create relative to the implementation root.</param>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    /// <returns>An <see cref="IBuilder"/> wrapped around <paramref name="builder"/> that prepends <paramref name="path"/> to paths.</returns>
    public static IBuilder BuildDirectory(this IBuilder builder, string? path)
    {
        if (string.IsNullOrEmpty(path)) return builder;
        builder.AddDirectory(path);
        return new PrefixBuilder(builder, path);
    }

    /// <summary>
    /// Adds a file to the implementation.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="metadata">The metadata of the file.</param>
    /// <param name="stream">The contents of the file.</param>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    public static void AddFile(this IForwardOnlyBuilder builder, SingleFile metadata, Stream stream)
        => builder.AddFile(metadata.Destination, stream, 0, metadata.Executable);

    /// <summary>
    /// Removes a file or directory from the implementation.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="metadata">The path of the file or directory.</param>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    public static void Remove(this IBuilder builder, RemoveStep metadata)
        => builder.Remove(metadata.Path);

    /// <summary>
    /// Renames a file or directory in the implementation.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="metadata">The path of the source and destination file or directory.</param>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    public static void Rename(this IBuilder builder, RenameStep metadata)
        => builder.Rename(metadata.Source, metadata.Destination);

    /// <summary>
    /// Copies files or directories from another implementation.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="metadata">The path of the source and destination file or directory.</param>
    /// <param name="path">The path of the implementation referenced by <paramref name="metadata"/>.</param>
    /// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
    /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
    /// <exception cref="IOException">An IO operation failed.</exception>
    public static void CopyFrom(this IBuilder builder, CopyFromStep metadata, string path, ITaskHandler handler)
        => handler.RunTask(new ReadDirectory(path, builder.BuildDirectory(metadata.Destination), Resources.CopyFiles));
}
