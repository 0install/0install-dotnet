// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts implementation archives.
/// </summary>
[PrimaryConstructor]
public abstract partial class ArchiveExtractor : IArchiveExtractor
{
    private static readonly Dictionary<string, Func<ITaskHandler, IArchiveExtractor>> _factories = new();

    /// <summary>
    /// Registers an additional <see cref="IArchiveExtractor"/>.
    /// </summary>
    /// <param name="mimeType">The MIME type of archive format the extractor handles.</param>
    /// <param name="factory">Callback providing instances of the extractor.</param>
    public static void Register(string mimeType, Func<ITaskHandler, IArchiveExtractor> factory)
        => _factories.Add(mimeType, factory);

    static ArchiveExtractor()
    {
        Register(Archive.MimeTypeZip, handler => new ZipExtractor(handler));
        Register(Archive.MimeTypeTar, handler => new TarExtractor(handler));
        Register(Archive.MimeTypeTarGzip, handler => new TarGzExtractor(handler));
        Register(Archive.MimeTypeTarBzip, handler => new TarBz2Extractor(handler));
        Register(Archive.MimeTypeTarLzma, handler => new TarLzmaExtractor(handler));
        Register(Archive.MimeTypeTarLzip, handler => new TarLzipExtractor(handler));
        Register(Archive.MimeTypeTarXz, handler => new TarXzExtractor(handler));
        Register(Archive.MimeTypeTarZstandard, handler => new TarZstandardExtractor(handler));
        Register(Archive.MimeType7Z, handler => new SevenZipExtractor(handler));
        Register(Archive.MimeTypeRar, handler => new RarExtractor(handler));
#if NETFRAMEWORK
        Register(Archive.MimeTypeCab, handler => new CabExtractor(handler));
        Register(Archive.MimeTypeMsi, handler => new MsiExtractor(handler));
#endif
        Register(Archive.MimeTypeRubyGem, handler => new RubyGemExtractor(handler));
        Register(Archive.MimeTypeDmg, handler => new DmgExtractor(handler));
    }

    /// <summary>
    /// Creates a new <see cref="IArchiveExtractor"/> for a specific type of archive.
    /// </summary>
    /// <param name="mimeType">The MIME type of archive format to extract.</param>
    /// <param name="handler">A callback object used when the the user needs to be informed about IO tasks.</param>
    /// <exception cref="NotSupportedException">No extractor registered for <paramref name="mimeType"/>.</exception>
    public static IArchiveExtractor For(string mimeType, ITaskHandler handler)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(mimeType)) throw new ArgumentNullException(nameof(mimeType));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        #endregion

        if (!_factories.TryGetValue(mimeType, out var factory))
            throw new NotSupportedException(string.Format(Resources.UnsupportedArchiveMimeType, mimeType));

        return factory(handler);
    }

    /// <summary>
    /// A callback object used when the the user needs to be informed about IO tasks.
    /// </summary>
    protected readonly ITaskHandler Handler;

    /// <inheritdoc/>
    public abstract void Extract(IBuilder builder, Stream stream, string? subDir = null);

    /// <inheritdoc/>
    public object? Tag { get; set; }

    /// <summary>
    /// Ensures that a <see cref="Stream"/> is fully seekable, creating a temporary on-disk copy if necessary.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="callback">Called with the original <paramref name="stream"/> or a temporary seekable copy.</param>
    protected void EnsureSeekable(Stream stream, [InstantHandle] Action<Stream> callback)
    {
        if (stream.CanSeek)
            callback(stream);
        else
        {
            using var tempFile = new TemporaryFile("0install-archive");
            stream.CopyToFile(tempFile);
            Handler.RunTask(new ReadFile(tempFile, callback, Resources.ExtractingArchive) {Tag = Tag});
        }
    }

    /// <summary>
    /// Ensures that a <see cref="Stream"/> represents an on-disk file, creating a temporary on-disk copy if necessary.
    /// </summary>
    /// <param name="stream">The stream to read. May be <see cref="Stream.Close"/>d.</param>
    /// <param name="callback">Called with the file path.</param>
    protected static void EnsureFile(Stream stream, [InstantHandle] Action<string> callback)
    {
        if (stream is FileStream fileStream)
        {
            fileStream.Close();
            callback(fileStream.Name);
        }
        else
        {
            using var tempFile = new TemporaryFile("0install-archive");
            stream.CopyToFile(tempFile);
            callback(tempFile);
        }
    }

    /// <summary>
    /// Normalizes the path of an archive entry.
    /// </summary>
    /// <param name="path">The Unix-style path of the archive entry relative to the archive's root.</param>
    /// <param name="subDir">The Unix-style path of the subdirectory in the archive to extract; <c>null</c> to extract entire archive.</param>
    /// <returns>The relative path without the <paramref name="subDir"/>; <c>null</c> if the <paramref name="path"/> doesn't lie within the <paramref name="subDir"/>.</returns>
    protected static string? NormalizePath(string path, string? subDir)
    {
        path = path.ToNativePath().Trim(Path.DirectorySeparatorChar);
        if (path.StartsWith("." + Path.DirectorySeparatorChar, out string? rest))
            path = rest;
        if (path == ".") return null;

        if (string.IsNullOrEmpty(subDir)) return path;
        subDir = subDir.ToNativePath().Trim(Path.DirectorySeparatorChar);
        if (path.StartsWith(subDir + Path.DirectorySeparatorChar, out rest))
            return rest;
        return null;
    }
}
