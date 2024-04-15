// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Builders;

/// <summary>
/// Builds implementation archive files.
/// </summary>
public static class ArchiveBuilder
{
    /// <summary>
    /// All supported MIME types for creating archives. This is a subset of <see cref="Archive.KnownMimeTypes"/>
    /// </summary>
    public static readonly string[] SupportedMimeTypes = [Archive.MimeTypeZip, Archive.MimeTypeTar, Archive.MimeTypeTarGzip, Archive.MimeTypeTarBzip, Archive.MimeTypeTarLzip, Archive.MimeTypeTarZstandard];

    /// <summary>
    /// Creates a new <see cref="ArchiveBuilder"/> for creating an archive and writing it to a stream.
    /// </summary>
    /// <param name="stream">The stream to write the archive to. Will be disposed when the builder is disposed.</param>
    /// <param name="mimeType">The MIME type of archive format to create.</param>
    /// <param name="fast">The compression operation should complete as quickly as possible, even if the resulting file is not optimally compressed.</param>
    /// <exception cref="NotSupportedException">The <paramref name="mimeType"/> doesn't belong to a known and supported archive type.</exception>
    [MustDisposeResource]
    public static IArchiveBuilder Create(Stream stream, string mimeType, bool fast = false)
    {
        #region Sanity checks
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
        #endregion

        return mimeType switch
        {
            Archive.MimeTypeZip => new ZipBuilder(stream),
            Archive.MimeTypeTar => new TarBuilder(stream),
            Archive.MimeTypeTarGzip => new TarGzBuilder(stream, fast),
            Archive.MimeTypeTarBzip => new TarBz2Builder(stream, fast),
            Archive.MimeTypeTarLzip when fast => throw new NotSupportedException($"{mimeType} is not supported here because the compression is too slow."),
            Archive.MimeTypeTarLzip => new TarLzipBuilder(stream),
            Archive.MimeTypeTarZstandard => new TarZstandardBuilder(stream, fast),
            _ => throw new NotSupportedException(string.Format(Resources.UnsupportedArchiveMimeType, mimeType))
        };
    }

    /// <summary>
    /// Creates a new <see cref="ArchiveBuilder"/> for creating an archive and writing it to a file.
    /// </summary>
    /// <param name="path">The path of the archive file to create.</param>
    /// <param name="mimeType">The MIME type of archive format to create.</param>
    /// <param name="fast">The compression operation should complete as quickly as possible, even if the resulting file is not optimally compressed.</param>
    /// <exception cref="NotSupportedException">The <paramref name="mimeType"/> doesn't belong to a known and supported archive type.</exception>
    /// <exception cref="IOException">Failed to create the archive file.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the archive file was denied.</exception>
    [MustDisposeResource]
    public static IArchiveBuilder Create(string path, string mimeType, bool fast = false)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
        #endregion

        return Create(File.Create(path), mimeType, fast);
    }

    /// <summary>
    /// Create an an archive from a directory and writes it to a file.
    /// </summary>
    /// <param name="sourcePath">The path of the directory to read.</param>
    /// <param name="archivePath">The path of the archive file to create.</param>
    /// <param name="mimeType">The MIME type of archive format to create.</param>
    /// <param name="handler">A callback object used when the user needs to be informed about IO tasks.</param>
    /// <exception cref="NotSupportedException">The <paramref name="mimeType"/> doesn't belong to a known and supported archive type.</exception>
    /// <exception cref="IOException">Failed to read the directory or create the archive file.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the directory or write access to the archive file was denied.</exception>
    public static void RunForDirectory(string sourcePath, string archivePath, string mimeType, ITaskHandler handler)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));
        if (string.IsNullOrEmpty(archivePath)) throw new ArgumentNullException(nameof(archivePath));
        if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        #endregion

        Log.Debug($"Creating archive '{archivePath}' from directory '{sourcePath}");
        using var builder = Create(archivePath, mimeType);
        handler.RunTask(new ReadDirectory(sourcePath, builder, string.Format(Resources.BuildingArchive, archivePath)));
    }
}
