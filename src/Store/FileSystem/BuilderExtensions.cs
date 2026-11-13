// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.FileSystem;

/// <summary>
/// Helpers for adding <see cref="RetrievalMethod"/>s to <see cref="IBuilder"/>s.
/// </summary>
public static class BuilderExtensions
{
    /// <param name="builder">The builder.</param>
    extension(IForwardOnlyBuilder builder)
    {
        /// <summary>
        /// Adds a file to the implementation.
        /// </summary>
        /// <param name="path">The path of the file to create relative to the implementation root.</param>
        /// <param name="file">The file object used to read the contents and metadata.</param>
        /// <param name="manifestElement">The file's equivalent manifest entry, if available.</param>
        /// <param name="hardlinkTarget">The path of a previously handled file relative to the implementation root that is hardlinked to <paramref name="file"/>. May be <c>null</c>.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        public void AddFile(string path, FileInfo file, ManifestElement? manifestElement = null, string? hardlinkTarget = null)
        {
            if (ImplFileUtils.IsSymlink(file.FullName, out string? symlinkTarget, manifestElement))
            {
                builder.AddSymlink(path, symlinkTarget);
                return;
            }

            if (!FileUtils.IsRegularFile(file.FullName))
                throw new NotSupportedException(string.Format(Resources.IllegalFileType, file.FullName));

            bool executable = ImplFileUtils.IsExecutable(file.FullName, manifestElement);
            try
            {
                if (hardlinkTarget != null)
                {
                    builder.AddHardlink(path, hardlinkTarget, executable);
                    return;
                }
            }
            catch (NotSupportedException)
            {}

            if (builder is IBuilder fullBuilder && fullBuilder.TryAddExternalHardlink(path, file, executable))
                return;

            using var stream = file.OpenRead();
            builder.AddFile(path, stream, file.LastWriteTimeUtc, executable);
        }

        /// <summary>
        /// Adds a file to the implementation.
        /// </summary>
        /// <param name="metadata">The metadata of the file.</param>
        /// <param name="stream">The contents of the file.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        public void AddFile(SingleFile metadata, Stream stream)
            => builder.AddFile(metadata.Destination, stream, 0, metadata.Executable);
    }

    /// <param name="builder">The builder.</param>
    extension(IBuilder builder)
    {
        /// <summary>
        /// Removes a file or directory from the implementation.
        /// </summary>
        /// <param name="metadata">The path of the file or directory.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        public void Remove(RemoveStep metadata)
            => builder.Remove(metadata.Path);

        /// <summary>
        /// Renames a file or directory in the implementation.
        /// </summary>
        /// <param name="metadata">The path of the source and destination file or directory.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        public void Rename(RenameStep metadata)
            => builder.Rename(metadata.Source, metadata.Destination);

        /// <summary>
        /// Copies files or directories from another implementation.
        /// </summary>
        /// <param name="metadata">The path of the source and destination file or directory.</param>
        /// <param name="path">The path of the implementation referenced by <paramref name="metadata"/>.</param>
        /// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        public void CopyFrom(CopyFromStep metadata, string path, ITaskHandler handler)
        {
            string? source = metadata.Source;
            string? destination = metadata.Destination;

            if (!string.IsNullOrEmpty(source)
             && new FileInfo(Paths.Combine(path, source.ToNativePath())) is {Exists: true} file)
            {
                var manifestElement = Manifest.TryLoad(Paths.Combine(path, Manifest.ManifestFile))
                                             ?.TryGetElement(source);

                handler.RunTask(new ActionTask(
                    Resources.CopyFiles,
                    () => builder.AddFile(destination.EmptyAsNull() ?? source, file, manifestElement)));
            }
            else
            {
                handler.RunTask(new ReadDirectory(
                    path,
                    subDir: source,
                    builder: builder.BuildDirectory(destination),
                    name: Resources.CopyFiles));
            }
        }

        /// <summary>
        /// Adds a subdirectory to the implementation and returns a wrapped <see cref="IBuilder"/> to elements inside this subdirectory.
        /// </summary>
        /// <param name="path">The path of the directory to create relative to the implementation root.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        /// <returns>An <see cref="IBuilder"/> wrapped around <paramref name="builder"/> that prepends <paramref name="path"/> to paths.</returns>
        public IBuilder BuildDirectory(string? path)
        {
            if (string.IsNullOrEmpty(path)) return builder;
            builder.AddDirectory(path);
            return new PrefixBuilder(builder, path);
        }
    }
}
